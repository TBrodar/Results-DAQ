using NationalInstruments.DAQmx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContinousAquisition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static public MainWindow WindowInstance;
        NI_DAQ Ni_DAQ_Instance = new NI_DAQ();
        public Graph GraphInstance = new Graph();
        Timer _timer;
        Timer _timer_Graph_Test;


        string Parameters_SaveFile = "Parameters.json";

        public Parameters Parameters_Instance = new Parameters
        {
            Output_File = "Output.txt",
            Sampling_Rate = 10000,
            NumberOfSamples = "Infinite",
            BufferSize = 10000,
            Reverse_Voltage = 0,
            Pulse_Voltage = 0,
            Pulse_Width = 1.0,

            Input_Channel = "Dev1/AI0",
            Input_Channel_MaxVoltage = 10,
            Input_Channel_MinVoltage = -10,
            Output_Channel = "Dev1/AO0",
            //Input_Trigger_Edge = "Rising",
            //Output_Trigger_Edge = "Falling",
            Output_Channel_MaxVoltage = 10,
            Output_Channel_MinVoltage = -10,
            Trigger_Channel = "PFI0",
            Sync_Channel = "Dev1/AO1",

            Max_Sampling_Rate_Limit = 1.25e6,
            Max_Buffer_Size_Limit = 32000,
            Min_Pulse_Width_Limit = 350e-9,
            Output_Channel_MinVoltage_Limit = -10,
            Output_Channel_MaxVoltage_Limit = 10,
            Input_Channel_MinVoltage_Limit  = -10,
            Input_Channel_MaxVoltage_Limit  = 10,
            Input_Channel_Value_Save_Format = "Text ({0:F6})",
            NumberOfMeasurements = "1"
        };
        delegate void void_Delegate();
        private void Add_If_List_Does_Not_Contain_It(List<string> List_Instance, string Object_To_Add)
        {
            if (List_Instance.Contains(Object_To_Add) == false)
            {
                List_Instance.Add(Object_To_Add);
            } 
        }

        public MainWindow()
        {
            InitializeComponent();

            // Load parameters
            Load_Parameters_func(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Parameters_SaveFile));
            // WPF binding
            this.DataContext = Parameters_Instance;
            WindowInstance = this;

            Add_If_List_Does_Not_Contain_It(Parameters_Instance.Output_Data_Format_Combobox_Items, "Text ({0:F6})");
            Add_If_List_Does_Not_Contain_It(Parameters_Instance.Output_Data_Format_Combobox_Items, "Binary (double values)");

            for (int i = 1; i <= 1000; i *= 10)
            {
                Add_If_List_Does_Not_Contain_It(Parameters_Instance.NumberOfMeasurements_ComboBox_Items, i.ToString()); 
            }
            for (int i = 1000; i <= 1000000; i *= 10)
            {
                Add_If_List_Does_Not_Contain_It(Parameters_Instance.Number_Of_Samples_ComboBox_Items, i.ToString());
            }
            Add_If_List_Does_Not_Contain_It(Parameters_Instance.Number_Of_Samples_ComboBox_Items, "Infinite");

            // Initialize graph
            GraphInstance.refresh(); 

            
            _timer = new Timer(Redraw_Graph, null, GraphInstance.CanvasParameters.RefreshDelayInMiliseconds, GraphInstance.CanvasParameters.RefreshDelayInMiliseconds);

            //_timer_Graph_Test = new Timer(AddPoint, null, 1, 1);

        }
        
        private void Redraw_Graph(Object state)
        {
            Thread.Sleep(10);
            MainWindow.WindowInstance.Dispatcher.BeginInvoke(new void_Delegate(GraphInstance.refresh)
                                       );
            _timer.Change(GraphInstance.CanvasParameters.RefreshDelayInMiliseconds, GraphInstance.CanvasParameters.RefreshDelayInMiliseconds);
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            // Check Parameters

            // Apply reverse voltage ?
            if (ApplyReverseVoltage_ToogleButton.IsChecked == false)
            {
                var Answer = MessageBox.Show("Apply reverse voltage before 1-st pulse?", "Question", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (Answer == MessageBoxResult.Yes)
                {
                    ApplyReverseVoltage_ToogleButton.IsChecked = true;
                    ApplyReverseVoltage_ToogleButton_Click(new object(), new RoutedEventArgs());
                    return;
                } else if (Answer == MessageBoxResult.Cancel )
                {
                    return;
                }
            }

            string ErrorMessages = "";
            int NumberOfMeasurements;
            try //NumberOfMeasurements_ComboBox
            {
                NumberOfMeasurements = int.Parse(NumberOfMeasurements_ComboBox.Text);
                if (NumberOfMeasurements < 1)
                {
                    ErrorMessages += "Number of measurements should be larger than zero. Please enter valid value.\n";
                }
                else
                {
                    if (Parameters_Instance.NumberOfMeasurements_ComboBox_Items.Contains( NumberOfMeasurements.ToString()) == false)
                    {
                        Parameters_Instance.NumberOfMeasurements_ComboBox_Items.Add(NumberOfMeasurements.ToString());
                    }
                    Parameters_Instance.NumberOfMeasurements = NumberOfMeasurements.ToString();
                }

            }
            catch (Exception)
            {
                ErrorMessages += "Number of measurements is not a integer number. Please enter valid value.\n";
                MessageBox.Show(ErrorMessages);
                return;
            }

            // check Output file name
            if (Parameters_Instance.Output_File.Length - 4 > 0 &&  Parameters_Instance.Output_File.Substring(Parameters_Instance.Output_File.Length - 4).ToLower() == ".txt")
            { Parameters_Instance.Output_File = Parameters_Instance.Output_File.Substring(0, Parameters_Instance.Output_File.Length - 4); }

            string Number_Of_measurements_Measured_string = "1";
            string Existing_files = "";
            for (int i = 0; i < NumberOfMeasurements; i++)
            {
                if (File.Exists(Parameters_Instance.Output_File + "_" + Number_Of_measurements_Measured_string + ".txt"))
                {
                    Existing_files += Parameters_Instance.Output_File + "_" + Number_Of_measurements_Measured_string + ".txt\n";
                }
                Number_Of_measurements_Measured_string = (int.Parse(Number_Of_measurements_Measured_string) + 1).ToString(); 
            }

            if (Existing_files != "")
            {
                var Answer = MessageBox.Show("Replace existing measurements?\n" + Existing_files, "Question", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (Answer == MessageBoxResult.Cancel)
                {
                    return;
                }
                else
                {
                    Number_Of_measurements_Measured_string = "0";
                    for (int i = 0; i < NumberOfMeasurements; i++)
                    {
                        Number_Of_measurements_Measured_string = (int.Parse(Number_Of_measurements_Measured_string) + 1).ToString();
                        using (System.IO.StreamWriter file =
                            new System.IO.StreamWriter(Parameters_Instance.Output_File + "_" + Number_Of_measurements_Measured_string + ".txt", false))
                        {
                        }
                    }
                    
                }
            }

            try // Sampling rate
            {
                double Sampling_Rate = double.Parse(Sampling_Rate_TextBox.Text);
                if (Sampling_Rate > Parameters_Instance.Max_Sampling_Rate_Limit)
                {
                    ErrorMessages += "Sampling rate is too large. Please enter valid value.\n"; 
                }
            } catch (Exception)
            {
                ErrorMessages += "Sampling rate is not a number. Please enter valid value.\n"; 
            }
            try // Buffer Size
            {
                double Buffer_Size = int.Parse(Buffer_Size_TextBox.Text);
                if (Buffer_Size > Parameters_Instance.Max_Buffer_Size_Limit)
                {
                    ErrorMessages += "Buffer size is too large. Please enter valid value.\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Buffer size is not a integer number. Please enter valid value.\n"; 
            }

            

            try //NumberOfMeasurements_ComboBox
            {
                int Number_Of_Samples = int.Parse(Number_Of_Samples_ComboBox.Text);
                if (Number_Of_Samples < 1)
                {
                    ErrorMessages += "Number of samples should be larger than zero. Please enter valid value.\n";
                } else
                {
                    Parameters_Instance.NumberOfSamples = Number_Of_Samples.ToString();
                }
            }
            catch (Exception)
            {
                if (Number_Of_Samples_ComboBox.Text != "Infinite")
                {
                    ErrorMessages += "Number of samples should be set to integer number or 'Infinite'. Please enter valid value.\n" + Number_Of_Samples_ComboBox.Text + "\n";

                }
            }

            // TODO:: Number of points, number of scans

            try // Reverse_Voltage_TextBox
            {
                double Reverse_Voltage = double.Parse(Reverse_Voltage_TextBox.Text);
                if (Reverse_Voltage > Parameters_Instance.Output_Channel_MaxVoltage || Reverse_Voltage < Parameters_Instance.Output_Channel_MinVoltage)
                {
                    ErrorMessages += "Please enter valid reverse voltage value (Min. output voltage < reverse voltage < Max. output voltage).\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Reverse voltage is not a number. Please enter valid value.\n";
            }

            try // Pulse_Voltage_TextBox
            {
                double Pulse_Voltage = double.Parse(Pulse_Voltage_TextBox.Text);
                if (Pulse_Voltage > Parameters_Instance.Output_Channel_MaxVoltage || Pulse_Voltage < Parameters_Instance.Output_Channel_MinVoltage)
                {
                    ErrorMessages += "Please enter valid pulse voltage value (Min. output voltage < pulse voltage < Max. output voltage).\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Pulse voltage is not a number. Please enter valid value.\n"; 
            }
            try // Pulse_Width_TextBox
            {
                double Pulse_Width = double.Parse(Pulse_Width_TextBox.Text);
                if (Pulse_Width < Parameters_Instance.Min_Pulse_Width_Limit)
                {
                    ErrorMessages += "Please enter larger pulse width value.\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Pulse width is not a number. Please enter valid value."; 
            }

            // Channel settings
            try // InputChannel_maxVoltage
            {
                double InputChannel_maxVoltage_value = double.Parse(InputChannel_maxVoltage.Text);
                if (InputChannel_maxVoltage_value > Parameters_Instance.Input_Channel_MaxVoltage_Limit || InputChannel_maxVoltage_value < Parameters_Instance.Input_Channel_MinVoltage_Limit)
                {
                    ErrorMessages += "Please enter a value of max. voltage at input channel within lower and upper limit. (Input_Channel_MinVoltage_Limit<= x <= Input_Channel_MaxVoltage_Limit)\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Max. voltage at input channel is not a number. Please enter valid value.\n";
            }
            try // InputChannel_minVoltage
            {
                double InputChannel_minVoltage_value = double.Parse(InputChannel_minVoltage.Text);
                if (InputChannel_minVoltage_value > Parameters_Instance.Input_Channel_MaxVoltage_Limit || InputChannel_minVoltage_value < Parameters_Instance.Input_Channel_MinVoltage_Limit)
                {
                    ErrorMessages += "Please enter a value of min. voltage at input channel within lower and upper limit. (Input_Channel_MinVoltage_Limit<= x <= Input_Channel_MaxVoltage_Limit)\n"; 
                }
                if (InputChannel_minVoltage_value > Parameters_Instance.Input_Channel_MaxVoltage) // Must be after Input_Channel_MaxVoltage check
                {
                    ErrorMessages += "Please enter a larger value of max. voltage than min. voltage at input channel. (Input_Channel_MinVoltage< Input_Channel_MaxVoltage)\n"; 
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Max. voltage at input channel is not a number. Please enter valid value.\n"; 
            }

            try // OutputChannel_maxVoltage
            {
                double OutputChannel_maxVoltage_value = double.Parse(OutputChannel_maxVoltage.Text);
                if (OutputChannel_maxVoltage_value > Parameters_Instance.Output_Channel_MaxVoltage_Limit || OutputChannel_maxVoltage_value < Parameters_Instance.Output_Channel_MinVoltage_Limit)
                {
                    ErrorMessages += "Channel settings: Please enter a value of max. voltage at output channel within lower and upper limit. (Output_Channel_MinVoltage_Limit <= x <= Output_Channel_MaxVoltage_Limit)\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Max. voltage at input channel is not a number. Please enter valid value.\n"; 
            }
            try // OutputChannel_minVoltage
            {
                double OutputChannel_minVoltage_value = double.Parse(OutputChannel_minVoltage.Text);
                if (OutputChannel_minVoltage_value > Parameters_Instance.Output_Channel_MaxVoltage_Limit || OutputChannel_minVoltage_value < Parameters_Instance.Output_Channel_MinVoltage_Limit)
                {
                    ErrorMessages += "Please enter a value of min. voltage at input channel within lower and upper limit. (Input_Channel_MinVoltage_Limit<= x <= Input_Channel_MaxVoltage_Limit)\n"; 
                }
                if (OutputChannel_minVoltage_value > Parameters_Instance.Output_Channel_MaxVoltage) // Must be after Output_Channel_MaxVoltage check
                {
                    ErrorMessages += "Please enter a larger value of max. voltage than min. voltage at output channel. (Output_Channel_MinVoltage < Output_Channel_MaxVoltage)\n";
                }
            }
            catch (Exception)
            {
                ErrorMessages += "Max. voltage at input channel is not a number. Please enter valid value.\n"; 
            }
            if (ErrorMessages != "")
            {
                MessageBox.Show(ErrorMessages);
                return;
            }

            GraphInstance = new Graph();
            GraphInstance.Initialize_Graph();
            LogTextBlock.Text = ""; // Clear Log TextBlock
            Ni_DAQ_Instance.Configure_Start_Multiple_Measurements(Parameters_Instance);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.WindowInstance.Dispatcher.BeginInvoke(new MainWindow.Append_Log_Delegate(MainWindow.WindowInstance.Append_Log),
                                       "Stop_Click;\n"
                                       );

            Ni_DAQ_Instance.Running = false;
            if (Ni_DAQ_Instance.LoopThread != null) { Ni_DAQ_Instance.LoopThread.Interrupt(); } 
            Ni_DAQ_Instance.StopTask();
        }

        public delegate void Append_Log_Delegate(string text);
        public void Append_Log(string text)
        {
            LogTextBlock.Text += text;
        }

        private void CanGraph_Loaded(object sender, RoutedEventArgs e)
        {
            GraphInstance.Initialize_Graph();
        }

        private void CanGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GraphInstance.refresh();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Save_Parameters_func(Parameters_SaveFile);
        }

        private void ApplyReverseVoltage_ToogleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyReverseVoltage_ToogleButton.IsChecked == true)
            {
                Ni_DAQ_Instance.Apply_Voltage(Parameters_Instance, Parameters_Instance.Reverse_Voltage);
            } else
            {
                Ni_DAQ_Instance.Apply_Voltage(Parameters_Instance, 0);
            }
        }

        public void Enable_Parameters_Editing(bool isEnabled)
        {
            if (isEnabled == false)
            { 
                Run.IsEnabled = false;
                Output_File_TextBox.IsEnabled = false;
                Output_Data_Format_Combobox.IsEnabled = false;
                Comment.IsEnabled = false;
                Reverse_Voltage_TextBox.IsEnabled = false;
                Pulse_Voltage_TextBox.IsEnabled = false;
                Pulse_Width_TextBox.IsEnabled = false;
                ApplyReverseVoltage_ToogleButton.IsEnabled = false;
                Sampling_Rate_TextBox.IsEnabled = false;
                Buffer_Size_TextBox.IsEnabled = false;
                Number_Of_Samples_ComboBox.IsEnabled = false;
                NumberOfMeasurements_ComboBox.IsEnabled = false;
                
                Input_channel.IsEnabled = false;
                InputChannel_maxVoltage.IsEnabled = false;
                InputChannel_minVoltage.IsEnabled = false;
                Output_channel.IsEnabled = false;
                OutputChannel_maxVoltage.IsEnabled = false;
                OutputChannel_minVoltage.IsEnabled = false;
                Trigger_Channel.IsEnabled = false;
                Sync_Channel.IsEnabled = false; 
            } else
            {
                Run.IsEnabled = true;
                Output_File_TextBox.IsEnabled = true;
                Output_Data_Format_Combobox.IsEnabled = true;
                Comment.IsEnabled = true;
                Reverse_Voltage_TextBox.IsEnabled = true;
                Pulse_Voltage_TextBox.IsEnabled = true;
                Pulse_Width_TextBox.IsEnabled = true;
                ApplyReverseVoltage_ToogleButton.IsEnabled = true;
                Sampling_Rate_TextBox.IsEnabled = true;
                Buffer_Size_TextBox.IsEnabled = true;
                Number_Of_Samples_ComboBox.IsEnabled = true;
                NumberOfMeasurements_ComboBox.IsEnabled = true;

                Input_channel.IsEnabled = true;
                InputChannel_maxVoltage.IsEnabled = true;
                InputChannel_minVoltage.IsEnabled = true;
                Output_channel.IsEnabled = true;
                OutputChannel_maxVoltage.IsEnabled = true;
                OutputChannel_minVoltage.IsEnabled = true;
                Trigger_Channel.IsEnabled = true;
                Sync_Channel.IsEnabled = true;
            } 
        }

        private void Reverse_Voltage_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyReverseVoltage_ToogleButton.IsChecked = false;
        }
        
        private void Measuremets_Load_Save_Parameters_Click(object sender, RoutedEventArgs e)
        {
            if (this.Load_Save_Parameters.ContextMenu.IsOpen == false )
            {
                try
                {
                    this.Load_Save_Parameters.ContextMenu.Placement       = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    this.Load_Save_Parameters.ContextMenu.PlacementTarget = Load_Save_Parameters;
                    this.Load_Save_Parameters.ContextMenu.IsOpen          = true;
                }
                catch { } 
            }
            else
            { this.Load_Save_Parameters.ContextMenu.IsOpen = false; } 
        }


        public void Load_Parameters_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "json (.*json)|*.json|All(*.*)|*",
                Multiselect = false
            }; 

            if (openFileDialog.ShowDialog() == true)
            {
                string directory = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                string name = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);

                Load_Parameters_func(openFileDialog.FileName);
            }
        }

        private void Load_Parameters_func(string FileName)
        {
            if (File.Exists(FileName))
            {
                try
                {

                    Parameters_Instance = JsonConvert.DeserializeObject<Parameters>(File.ReadAllText(FileName));
                    this.DataContext = Parameters_Instance;
                }
                catch (Exception)
                {
                    MessageBox.Show("Parameters.json file is not properly loaded.");
                } 
            }
        }
        private void Save_Parameters_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "json (.*json)|*.json|All(*.*)|*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string directory = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                string name = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                Save_Parameters_func(System.IO.Path.Combine(directory, name + ".json"));
            }
        }

        public void Save_Parameters_func(string FileName)
        {
            if (!(Number_Of_Samples_ComboBox.Items.Contains(Number_Of_Samples_ComboBox.Text))){
                Parameters_Instance.Number_Of_Samples_ComboBox_Items.Add(Number_Of_Samples_ComboBox.Text);
                Parameters_Instance.NumberOfSamples = Number_Of_Samples_ComboBox.Text;
            }
            if (!(NumberOfMeasurements_ComboBox.Items.Contains(NumberOfMeasurements_ComboBox.Text)))
            {
                Parameters_Instance.NumberOfMeasurements_ComboBox_Items.Add(NumberOfMeasurements_ComboBox.Text);
                Parameters_Instance.NumberOfMeasurements = NumberOfMeasurements_ComboBox.Text;
            }
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(FileName))
            {
                file.Write(JsonConvert.SerializeObject(Parameters_Instance, Formatting.Indented));
            }
        } 
    } 
}
