using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Data;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System.Windows;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace ContinousAquisition
{
    class NI_DAQ
    {
        public Task inputTask = null;
        private Task outputTask = null;

        private AnalogSingleChannelReader Input_Reader ;
        private AnalogMultiChannelWriter Output_Writer;
        private AnalogSingleChannelWriter Output_Writer_Single;
        private AsyncCallback inputCallback;
        
        private Parameters _Parameters_Instance;

        private int Number_Of_Samples_Reqired;
        private int Number_Of_Samples_Measured;
        private int Number_Of_measurements_Reqired;
        private string Number_Of_measurements_Measured_string;

        private bool Is_Number_Of_Samples_Infinite;
        public Thread LoopThread;
        public bool Running;
        delegate void void_Delegate();
        private string Input_Channel_Value_Save_Format_Syntax;
        public void Configure_Start_Multiple_Measurements(Parameters Parameters_Instance
                                   )
        {
            
            MainWindow.WindowInstance.Enable_Parameters_Editing(false); // disable redundunt calls to this method
            Running = true;
            Number_Of_measurements_Measured_string = "0";
            Number_Of_measurements_Reqired = int.Parse(Parameters_Instance.NumberOfMeasurements);
            LoopThread = new Thread(() =>
            {
                while (int.Parse(Number_Of_measurements_Measured_string) < Number_Of_measurements_Reqired)
                {
                    try
                    {
                        if (Configure_Start_Single_Measurement(Parameters_Instance) != 0) { break; }
                        Number_Of_measurements_Measured_string = (int.Parse(Number_Of_measurements_Measured_string) + 1).ToString(); 
                        Thread.Sleep(Timeout.Infinite);
                    } catch (ThreadInterruptedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        StopTask();
                        break;
                    }
                    if(Running == false) { 
                        StopTask();
                        break;
                    }
                }

                MainWindow.WindowInstance.Dispatcher.BeginInvoke(new void_bool_Delegate(MainWindow.WindowInstance.Enable_Parameters_Editing),
                                           true
                                           );
            });
            LoopThread.Start();
        }
        delegate void void_bool_Delegate(bool IsEditable);
        public int Configure_Start_Single_Measurement( Parameters Parameters_Instance
                                   )
        {

            StopTask(); // ensure clear tasks before measurement

            string ParametersFileName = Parameters_Instance.Output_File + ".json";
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(ParametersFileName))
            {
                file.Write(JsonConvert.SerializeObject(Parameters_Instance, Formatting.Indented)); 
            }
             
            _Parameters_Instance = Parameters_Instance;
            if (_Parameters_Instance.NumberOfSamples == "Infinite")
            {
                Number_Of_Samples_Reqired = -1;
                Is_Number_Of_Samples_Infinite = true;
            } else
            {
                Is_Number_Of_Samples_Infinite = false;
                try
                {
                    Number_Of_Samples_Reqired = int.Parse(_Parameters_Instance.NumberOfSamples);
                } catch (Exception)
                {
                    StopTask();
                    MessageBox.Show("Number of samples is not an integer number or 'Infinite'.");
                    return 1;
                }
            }
            Number_Of_Samples_Measured = 0;

            // Find output data format in case of text output
            if (Parameters_Instance.Input_Channel_Value_Save_Format.Substring(0, 4).ToLower() == "text")
            { 
                int Index2 = Parameters_Instance.Input_Channel_Value_Save_Format.LastIndexOf(")");
                int Index1 = Parameters_Instance.Input_Channel_Value_Save_Format.IndexOf("(");
                Input_Channel_Value_Save_Format_Syntax = Parameters_Instance.Input_Channel_Value_Save_Format.Substring(Index1 + 1, Index2 - Index1 - 1);
                MainWindow.WindowInstance.Dispatcher.BeginInvoke(new MainWindow.Append_Log_Delegate(MainWindow.WindowInstance.Append_Log),
                                       "Output syntax:" + Input_Channel_Value_Save_Format_Syntax + "\n"
                                       );
            }

            // Sync pulse
            double Sync_PulseVoltage = +4.9; // [V]
            double Sync_IdileVoltage = 0.0;
            int NumberOfSamples_During_Pulse = 10;
            double WritingRate_Pulse = NumberOfSamples_During_Pulse / Parameters_Instance.Pulse_Width;


            double[,] Output_Data = new double[2, NumberOfSamples_During_Pulse + 1];
            for (int i = 0; i < NumberOfSamples_During_Pulse; i++)
            {
                Output_Data[0, i] = Sync_PulseVoltage;
                Output_Data[1, i] = Parameters_Instance.Pulse_Voltage;
            }
            Output_Data[0, NumberOfSamples_During_Pulse] = Sync_IdileVoltage;
            Output_Data[1, NumberOfSamples_During_Pulse] = Parameters_Instance.Reverse_Voltage;

            try
            {
                // Create the master and slave tasks
                inputTask  = new Task("InputTask");
                outputTask = new Task("OutputTask");
                
                // Configure both tasks with the values selected on the UI. #SyncTask
                inputTask.AIChannels.CreateVoltageChannel(Parameters_Instance.Input_Channel,
                    "InputChannel", 
                    AITerminalConfiguration.Differential,
                    Parameters_Instance.Input_Channel_MinVoltage,
                    Parameters_Instance.Input_Channel_MaxVoltage,
                    AIVoltageUnits.Volts);

                outputTask.AOChannels.CreateVoltageChannel(Parameters_Instance.Sync_Channel,
                    "",
                    Convert.ToDouble(0.0),
                    Convert.ToDouble(5.0),
                    AOVoltageUnits.Volts);
                outputTask.AOChannels.CreateVoltageChannel(Parameters_Instance.Output_Channel,
                    "",
                    Parameters_Instance.Output_Channel_MinVoltage,
                    Parameters_Instance.Output_Channel_MaxVoltage,
                    AOVoltageUnits.Volts);
               
                // Output pulse 
                inputTask.Timing.ConfigureSampleClock("",
                    Parameters_Instance.Sampling_Rate,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples,
                    Parameters_Instance.BufferSize);
                outputTask.Timing.ConfigureSampleClock("", 
                    WritingRate_Pulse,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.FiniteSamples,
                    NumberOfSamples_During_Pulse + 1);

                // Set up the start trigger
                DigitalEdgeStartTriggerEdge Input_triggerEdge = DigitalEdgeStartTriggerEdge.Falling; 
                inputTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(Parameters_Instance.Trigger_Channel,
                    Input_triggerEdge);
                                
                // Verify the tasks
                inputTask.Control(TaskAction.Verify);
                outputTask.Control(TaskAction.Verify);
                
                // Write data to each output channel
                Output_Writer = new AnalogMultiChannelWriter(outputTask.Stream);
                Output_Writer.WriteMultiSample(false, Output_Data);
                 
                inputTask.Start() ;

                inputCallback = new AsyncCallback(InputRead);
                Input_Reader = new AnalogSingleChannelReader(inputTask.Stream);
                //// Use SynchronizeCallbacks to specify that the object 
                //// marshals callbacks across threads appropriately.
                Input_Reader.SynchronizeCallbacks = true;
                Input_Reader.BeginReadMultiSample(Parameters_Instance.BufferSize, inputCallback, inputTask);

                // Generate Sync pulse
                
                //syncTask.Start();
                outputTask.Start();

                MainWindow.WindowInstance.Dispatcher.BeginInvoke(new MainWindow.Append_Log_Delegate(MainWindow.WindowInstance.Append_Log),
                                           "Done: Configure_Start " + Number_Of_measurements_Measured_string + "\n"
                                           );
                //inputTask.WaitUntilDone();
            }
            catch (Exception ex)
            {
                StopTask();
                MessageBox.Show(ex.Message);
                return 1;
            }
            return 0;
        }
        delegate void Add_Point_Delegate(double x, double y);
        private void InputRead(IAsyncResult ar)
        {
            try
            {
                if (inputTask != null && inputTask == ar.AsyncState)
                {
                    // Read the data
                    double[] data = Input_Reader.EndReadMultiSample(ar);
                    
                    
                   if ("Binary (double values)" == _Parameters_Instance.Input_Channel_Value_Save_Format)
                        {
                        using (BinaryWriter binWriter = new BinaryWriter(File.Open(_Parameters_Instance.Output_File + "_" + Number_Of_measurements_Measured_string + ".txt", FileMode.Append)))
                        {
                             
                            foreach (double d in data)
                                binWriter.Write(d);
                        }
                        } else
                        {
                            using (System.IO.StreamWriter file =
                               new System.IO.StreamWriter(_Parameters_Instance.Output_File + "_" + Number_Of_measurements_Measured_string + ".txt", true))
                            {
                                foreach (double d in data) { 
                                    file.WriteLine(String.Format(Input_Channel_Value_Save_Format_Syntax, d));
                            }
                        }
                    }
                    Number_Of_Samples_Measured += _Parameters_Instance.BufferSize; //TODO:: add Number_Of_Samples_Measured to parameters
                    
                    MainWindow.WindowInstance.Dispatcher.BeginInvoke(new  Add_Point_Delegate(MainWindow.WindowInstance.GraphInstance.Add_Point),
                                       Number_Of_Samples_Measured, data[data.Length - 1]
                                       );
                    if (Is_Number_Of_Samples_Infinite == false && Number_Of_Samples_Reqired <= Number_Of_Samples_Measured)
                    {
                        StopTask();
                        if (int.Parse(Number_Of_measurements_Measured_string) == Number_Of_measurements_Reqired)
                        {
                            Running = false;
                            //MainWindow.WindowInstance.GraphInstance.Clear_Data();
                        }
                        LoopThread.Interrupt();
                        return;
                    }
                    Input_Reader.BeginReadMultiSample(_Parameters_Instance.BufferSize, inputCallback, inputTask);
                }
            }
            catch (Exception ex)
            {
                StopTask();
                MessageBox.Show(ex.Message);
            }
        }

        public void StopTask()
        {  
            // Stop tasks
            if (inputTask != null )  { inputTask.Stop(); }
            if (outputTask != null) { outputTask.Stop(); } 

            if (inputTask != null)  { inputTask.Dispose();  inputTask  = null; }
            if (outputTask != null) { outputTask.Dispose(); outputTask = null; }
            MainWindow.WindowInstance.Dispatcher.BeginInvoke(new MainWindow.Append_Log_Delegate(MainWindow.WindowInstance.Append_Log),
                                       "Done: Stop tasks\n"
                                       ); 
        }

        public void Apply_Voltage(Parameters Parameters_Instance, double voltage)
        {

            MainWindow.WindowInstance.Dispatcher.BeginInvoke(new MainWindow.Append_Log_Delegate(MainWindow.WindowInstance.Append_Log),
                                       "Applling voltage " + voltage.ToString() + "\n"
                                       );
            try
            {
                outputTask = new Task("OutputTask");
                outputTask.AOChannels.CreateVoltageChannel(Parameters_Instance.Output_Channel,
                        "",
                        Parameters_Instance.Output_Channel_MinVoltage,
                        Parameters_Instance.Output_Channel_MaxVoltage,
                        AOVoltageUnits.Volts);

                outputTask.Control(TaskAction.Verify);
                // TODO:: check if timing is necessary
                Output_Writer_Single = new AnalogSingleChannelWriter(outputTask.Stream);
                Output_Writer_Single.WriteSingleSample(true, voltage);
                outputTask.Done += new TaskDoneEventHandler(Apply_Voltage_Done);
            }
            catch (Exception ex)
            {
                StopTask();
                MessageBox.Show(ex.Message);
            } finally
            {
                if (outputTask != null) { outputTask.Stop(); outputTask.Dispose(); outputTask = null; }
            }
        }
        private void Apply_Voltage_Done(object sender, TaskDoneEventArgs e)
        {
            outputTask.Stop();
            outputTask.Dispose();
            outputTask = null;
            MainWindow.WindowInstance.Dispatcher.BeginInvoke(new MainWindow.Append_Log_Delegate(MainWindow.WindowInstance.Append_Log),
                                       "Done: Apply voltage \n"
                                       );
        }
    }
}
