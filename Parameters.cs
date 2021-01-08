using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContinousAquisition
{
    public class Parameters
    {
        private string Output_File_Value;
        public string Output_File
        {
            get { return Output_File_Value; }
            set { Output_File_Value = value; }
        }

        private double Sampling_Rate_Value;
        public double Sampling_Rate
        {
            get { return Sampling_Rate_Value; }
            set { Sampling_Rate_Value = value; }
        }

        private string NumberOfSamples_Value;
        public string NumberOfSamples
        {
            get { return NumberOfSamples_Value; }
            set { NumberOfSamples_Value = value; }
        }

        private int BufferSize_Value;
        public int BufferSize
        {
            get { return BufferSize_Value; }
            set { BufferSize_Value = value; }
        }

        private string NumberOfMeasurements_Value;
        public string NumberOfMeasurements
        {
            get { return NumberOfMeasurements_Value; }
            set { NumberOfMeasurements_Value = value; }
        }

        private double Reverse_Voltage_Value;
        public double Reverse_Voltage
        {
            get { return Reverse_Voltage_Value; }
            set { Reverse_Voltage_Value = value; }
        }

        private double Pulse_Voltage_Value;
        public double Pulse_Voltage
        {
            get { return Pulse_Voltage_Value; }
            set { Pulse_Voltage_Value = value; }
        }

        private double Pulse_Width_Value;
        public double Pulse_Width
        {
            get { return Pulse_Width_Value; }
            set { Pulse_Width_Value = value; }
        }

        //private string Input_Trigger_Edge_Value;
        //public string Input_Trigger_Edge
        //{
        //    get { return Input_Trigger_Edge_Value; }
        //    set { Input_Trigger_Edge_Value = value; }
        //}
        //private string Output_Trigger_Edge_Value;
        //public string Output_Trigger_Edge
        //{
        //    get { return Output_Trigger_Edge_Value; }
        //    set { Output_Trigger_Edge_Value = value; }
        //}

        private string Output_Channel_Value;
        public string Output_Channel
        {
            get { return Output_Channel_Value; }
            set { Output_Channel_Value = value; }
        }
        private string Input_Channel_Value;
        public string Input_Channel
        {
            get { return Input_Channel_Value; }
            set { Input_Channel_Value = value; }
        }
        private double Input_Channel_MaxVoltage_Value;
        public double Input_Channel_MaxVoltage
        {
            get { return Input_Channel_MaxVoltage_Value; }
            set { Input_Channel_MaxVoltage_Value = value; }
        }
        private double Input_Channel_MinVoltage_Value;
        public double Input_Channel_MinVoltage
        {
            get { return Input_Channel_MinVoltage_Value; }
            set { Input_Channel_MinVoltage_Value = value; }
        }

        private double Output_Channel_MaxVoltage_Value;
        public double Output_Channel_MaxVoltage
        {
            get { return Output_Channel_MaxVoltage_Value; }
            set { Output_Channel_MaxVoltage_Value = value; }
        }
        private double Output_Channel_MinVoltage_Value;
        public double Output_Channel_MinVoltage
        {
            get { return Output_Channel_MinVoltage_Value; }
            set { Output_Channel_MinVoltage_Value = value; }
        }

        private string Trigger_Channel_Value;
        public string Trigger_Channel
        {
            get { return Trigger_Channel_Value; }
            set { Trigger_Channel_Value = value; }
        } 
        private string Sync_Channel_Value;
        public string Sync_Channel
        {
            get { return Sync_Channel_Value;  }
            set { Sync_Channel_Value = value; }
        }



        private double Max_Sampling_Rate_Value;
        public double Max_Sampling_Rate_Limit
        {
            get { return Max_Sampling_Rate_Value;  }
            set { Max_Sampling_Rate_Value = value; }
        }
        private double Max_Buffer_Size_Value;
        public double Max_Buffer_Size_Limit
        {
            get { return Max_Buffer_Size_Value; }
            set { Max_Buffer_Size_Value = value; }
        }
        private double Min_Pulse_Width_Value;
        public double Min_Pulse_Width_Limit
        {
            get { return Min_Pulse_Width_Value; }
            set { Min_Pulse_Width_Value = value; }
        }


        private double Output_Channel_MinVoltage_Limit_Value;
        public double Output_Channel_MinVoltage_Limit
        {
            get { return Output_Channel_MinVoltage_Limit_Value; }
            set { Output_Channel_MinVoltage_Limit_Value = value; }
        }
        private double Output_Channel_MaxVoltage_Limit_Value;
        public double Output_Channel_MaxVoltage_Limit
        {
            get { return Output_Channel_MaxVoltage_Limit_Value; }
            set { Output_Channel_MaxVoltage_Limit_Value = value; }
        }

        private double Intput_Channel_MinVoltage_Limit_Value;
        public double Input_Channel_MinVoltage_Limit
        {
            get { return Intput_Channel_MinVoltage_Limit_Value; }
            set { Intput_Channel_MinVoltage_Limit_Value = value; }
        }
        private double Input_Channel_MaxVoltage_Limit_Value;
        public double Input_Channel_MaxVoltage_Limit
        {
            get { return Input_Channel_MaxVoltage_Limit_Value; }
            set { Input_Channel_MaxVoltage_Limit_Value = value; }
        }

        private string Input_Channel_Value_Save_Format_Value;
        public string Input_Channel_Value_Save_Format
        {
            get { return Input_Channel_Value_Save_Format_Value; }
            set { Input_Channel_Value_Save_Format_Value = value; }
        }

        private List<string> Output_Data_Format_Combobox_Items_Values = new List<string>();
        public List<string> Output_Data_Format_Combobox_Items
        {
            get { return Output_Data_Format_Combobox_Items_Values;  }
            set { Output_Data_Format_Combobox_Items_Values = value; }
        }

        private List<string> Number_Of_Samples_ComboBox_Items_Values = new List<string>();
        public List<string> Number_Of_Samples_ComboBox_Items
        {
            get { return Number_Of_Samples_ComboBox_Items_Values; }
            set { Number_Of_Samples_ComboBox_Items_Values = value; }
        }

        private List<string> NumberOfMeasurements_ComboBox_Items_Values = new List<string>();
        public List<string> NumberOfMeasurements_ComboBox_Items
        {
            get { return NumberOfMeasurements_ComboBox_Items_Values; }
            set { NumberOfMeasurements_ComboBox_Items_Values = value; }
        }
          
    }
}
