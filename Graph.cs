using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ContinousAquisition
{ 
    public class Graph
    {
        public class Graph_Parameters
        {
            public double x_margin          = 0.1;
            public double y_margin          = 0.1;
            public double StrokeThickness   = 1.0;
            public double Overhead          = 0.001;
            public int Number_Of_Points_Shown = 100;
            public double X_Axis_Zoom_Factor = 0.1; // 0 -> xmin = min(X), xmax = max(X); 2 -> xmin = 2*min(X), xmax = 2*max(X)
            public double Y_Axis_Zoom_Factor = 0.1; // 0 -> xmin = min(X), xmax = max(X); 2 -> xmin = 2*min(X), xmax = 2*max(X)
            public double LineTickness = 1.0;
            public string X_Label = "Sample number";
            public string Y_Label = "Voltage (V)";
            public double Label_Size = 20.0;
            public double Tick_Label_Size = 12.0;
            public double X_Label_Position = 0.7;
            public double Y_Label_Position = 0.7;
            public int RefreshDelayInMiliseconds = 100;
            public double Major_Tick_Length = 0.012;
            public double Minior_Tick_Length = 0.0075;
            //public enum PlotTypes = { "Moving", "Average"};
        }
        public Graph_Parameters CanvasParameters;
        public LinkedList<Point > Data_Points = new LinkedList<Point > {};
        double xmin;
        double xmax;
        double ymin;
        double ymax;
        TextBlock X_Label_TextBlock;
        TextBlock Y_Label_TextBlock;
        int WhichEveryNthPoint = 1;
        TextBlock[] X_tick_Labels = new TextBlock[11];
        TextBlock[] Y_tick_Labels = new TextBlock[11];

        public void Initialize_Graph()
        {
            MainWindow.WindowInstance.canGraph.Children.Clear();
            Canvas CanvasInstance = MainWindow.WindowInstance.canGraph;

            CanvasParameters = new Graph_Parameters();
            
            xmin = CanvasInstance.Width*CanvasParameters.x_margin;
            xmax = CanvasInstance.Width*(1.0 - CanvasParameters.x_margin);
            ymin = CanvasInstance.Height * CanvasParameters.y_margin;
            ymax = CanvasInstance.Height * (1.0 - CanvasParameters.y_margin); 

            // Make the X axis.
            GeometryGroup axis_geom = new GeometryGroup();
            axis_geom.Children.Add(new LineGeometry(
                new Point(xmin - xmax * CanvasParameters.Overhead, ymax), new Point(xmax + xmax * CanvasParameters.Overhead, ymax)));
            axis_geom.Children.Add(new LineGeometry(
                new Point(xmin - xmax * CanvasParameters.Overhead, ymin), new Point(xmax + xmax * CanvasParameters.Overhead, ymin)));
            axis_geom.Children.Add(new LineGeometry(
                new Point(xmin, ymin), new Point(xmin, ymax)));
            axis_geom.Children.Add(new LineGeometry(
                new Point(xmax, ymin), new Point(xmax, ymax)));

            Path xaxis_path = new Path();
            xaxis_path.StrokeThickness = CanvasParameters.StrokeThickness;
            xaxis_path.Stroke = Brushes.Black;
            xaxis_path.Data = axis_geom;

            CanvasInstance.Children.Add(xaxis_path);

            if (CanvasParameters.X_Label != ""  && CanvasParameters.X_Label != null)
            {
                X_Label_TextBlock = new TextBlock();
                X_Label_TextBlock.Text       = CanvasParameters.X_Label;
                X_Label_TextBlock.FontSize   = CanvasParameters.Label_Size;
                X_Label_TextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0,0,0)); 
                CanvasInstance.Children.Add(X_Label_TextBlock);

                //Canvas.SetLeft(X_Label_TextBlock, xmin + (xmax - xmin) / 2.0 - X_Label_TextBlock.ActualWidth); 
                //Canvas.SetTop(X_Label_TextBlock, CanvasInstance.Height * (1.0 - CanvasParameters.y_margin*CanvasParameters.X_Label_Position));

            }
            if (CanvasParameters.Y_Label != "" && CanvasParameters.Y_Label != null)
            {
                Y_Label_TextBlock = new TextBlock();
                Y_Label_TextBlock.Text = CanvasParameters.Y_Label;
                Y_Label_TextBlock.FontSize = CanvasParameters.Label_Size;
                Y_Label_TextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));

                //Canvas.SetLeft(Y_Label_TextBlock, xmin * (1.0 - CanvasParameters.Y_Label_Position));
                //Canvas.SetTop(Y_Label_TextBlock, ymin + (ymax - ymin) / 2.0 - Y_Label_TextBlock.ActualWidth);
                var rotateAnimation = new DoubleAnimation(0, -90, TimeSpan.FromSeconds(0));
                //textBlock_y.RenderTransformOrigin = new Point(0.5, 0.5); 
                Y_Label_TextBlock.RenderTransform = new RotateTransform();
                Y_Label_TextBlock.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
                Y_Label_TextBlock.Loaded += new RoutedEventHandler(Labels_Loaded);
                CanvasInstance.Children.Add(Y_Label_TextBlock);  
            }


        }
        private void Labels_Loaded(object sender, RoutedEventArgs e)
        {

            double YLabel_xpos = xmin *(1.0 - CanvasParameters.Y_Label_Position);
            if (YLabel_xpos + Y_Label_TextBlock.ActualHeight > xmin)
            {
                YLabel_xpos = xmin - Y_Label_TextBlock.ActualHeight;
            }
            Canvas.SetLeft(Y_Label_TextBlock, YLabel_xpos);
            Canvas.SetTop(Y_Label_TextBlock, ymin + (ymax - ymin) / 2.0 + Y_Label_TextBlock.ActualWidth/2.0);

            Canvas.SetLeft(X_Label_TextBlock, xmin + (xmax - xmin) / 2.0 - X_Label_TextBlock.ActualWidth/2.0);
            Canvas.SetTop(X_Label_TextBlock, MainWindow.WindowInstance.canGraph.Height * (1.0 - CanvasParameters.y_margin * CanvasParameters.X_Label_Position));
            MainWindow.WindowInstance.Dispatcher.Invoke((Action)(() =>
            {
                MainWindow.WindowInstance.canGraph.UpdateLayout();  
            }));
        }


        public void refresh()
        { 
            Initialize_Graph();
            Plot_Data();

        }
        int _Shipped_Number_Of_Points = 1;

        
        public void Add_Point(double x, double y)
        {

            if (_Shipped_Number_Of_Points % WhichEveryNthPoint == 0)
            {
                Data_Points.AddLast(new Point(x, y));
                _Shipped_Number_Of_Points = 1;
            } else
            {
                _Shipped_Number_Of_Points += 1;
            }
        }

        //bool clearData = false;
        //public void Clear_Data()
        //{
        //    clearData = true;
        //}
        public void Plot_Data()
        {
            //if (clearData == true)
            //{
            //    Data_Points.Clear();
            //    Data_Points = new LinkedList<Point>();
            //    clearData = false;
            //    return;
            //}
            if (Data_Points.Count < 3) { return; }
            if ( Data_Points.Count > CanvasParameters.Number_Of_Points_Shown)
            {
                for (LinkedListNode<Point> Point = Data_Points.First; Point != null &&  Point.Next != null; Point = Point.Next)
                {
                    Data_Points.Remove(Point.Next); 
                }
                WhichEveryNthPoint *= 2;
            }

            // Find min-max values
            double Data_x_min = Data_Points.First.Value.X;
            double Data_x_max = Data_Points.Last.Value.X;
            double Data_y_min = Data_Points.First.Value.Y;
            double Data_y_max = Data_Points.Last.Value.Y;
            for (LinkedListNode<Point> Point = Data_Points.First; Point != null; Point = Point.Next)
            { 
                if (Point.Value.Y < Data_y_min) { Data_y_min = Point.Value.Y; }
                if (Point.Value.Y > Data_y_max) { Data_y_max = Point.Value.Y; }
            }

            // Map points to graph
            //double x_min_axis_pad = (Data_x_max - Data_x_min)*CanvasParameters.X_Axis_Zoom_Factor;
            double y_min_axis_pad = ((Data_y_max - Data_y_min)*CanvasParameters.Y_Axis_Zoom_Factor)/2.0;
            double x_factor = (xmax - xmin) / (Data_x_max - Data_x_min);
            double y_factor = (ymax - ymin) / ( (Data_y_max + y_min_axis_pad) - (Data_y_min - y_min_axis_pad)); 
            LinkedList<Point> Plot_Points = new LinkedList<Point>();
            for (LinkedListNode<Point> Point = Data_Points.First; Point != null; Point = Point.Next)
            { 
                double x = x_factor*(Point.Value.X - Data_x_min )                    + xmin;
                double y = - y_factor*(Point.Value.Y - (Data_y_min - y_min_axis_pad) ) + ymax;
                Plot_Points.AddLast(new Point(x, y));
            }

            // Assumes ascending order of data
            GeometryGroup line_geom = new GeometryGroup();
            for (LinkedListNode<Point> Point = Plot_Points.First; Point.Next != null; Point = Point.Next)
            {
                line_geom.Children.Add(new LineGeometry(Point.Value, Point.Next.Value));
            }

            Path path = new Path();
            path.StrokeThickness = CanvasParameters.LineTickness;
            path.Stroke = Brushes.Navy;
            path.Data = line_geom;
                                 
            MainWindow.WindowInstance.canGraph.Children.Add(path);



            // Add ticks and tick labels
            // X axis
            double OrderOfMagnitude = Math.Floor(Math.Log10(Data_x_max - Data_x_min)); // Get order of magnitude
            double Major_tick_period = Math.Pow(10, OrderOfMagnitude);
            int Number_Of_Major_Ticks = (int)Math.Ceiling((Data_x_max - Data_x_min) / Major_tick_period); 
            for (int i = 0; i < Number_Of_Major_Ticks; i++)
            {
                double xtick_location = x_factor * (i * Major_tick_period - Data_x_min) + xmin;
                if (xmax < xtick_location) { continue; }
                Line line = new Line();
                line.X1 = xtick_location;
                line.Y1 = ymax;
                line.X2 = xtick_location;
                line.Y2 = ymax - (ymax - ymin) * CanvasParameters.Major_Tick_Length;
                line.StrokeThickness = CanvasParameters.LineTickness;
                line.Stroke = Brushes.Black;
                MainWindow.WindowInstance.canGraph.Children.Add(line);
                Line line2 = new Line();
                line2.X1 = xtick_location;
                line2.Y1 = ymin;
                line2.X2 = xtick_location;
                line2.Y2 = ymin + (ymax - ymin) * CanvasParameters.Major_Tick_Length;
                line2.StrokeThickness = CanvasParameters.LineTickness;
                line2.Stroke = Brushes.Black;
                MainWindow.WindowInstance.canGraph.Children.Add(line2);

                X_tick_Labels[i] = new TextBlock();
                X_tick_Labels[i].Text =  ((long)(i * Major_tick_period)).ToString("D");
                X_tick_Labels[i].FontSize = CanvasParameters.Label_Size;
                X_tick_Labels[i].Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                MainWindow.WindowInstance.canGraph.Children.Add(X_tick_Labels[i]);
                Canvas.SetLeft(X_tick_Labels[i], xtick_location - X_tick_Labels[i].ActualWidth/2.0);
                Canvas.SetTop(X_tick_Labels[i], line.Y1 );
            }
            for (int i = 0; i < Number_Of_Major_Ticks; i++)
            {
                double xtick_location = x_factor * ((i + 0.5) * Major_tick_period - Data_x_min) + xmin;
                if (xmax < xtick_location) { continue; }
                Line line = new Line();
                line.X1 = xtick_location;
                line.Y1 = ymax;
                line.X2 = xtick_location;
                line.Y2 = ymax - (ymax - ymin) * CanvasParameters.Minior_Tick_Length;
                line.StrokeThickness = CanvasParameters.LineTickness;
                line.Stroke = Brushes.Black;
                MainWindow.WindowInstance.canGraph.Children.Add(line);
                Line line2 = new Line();
                line2.X1 = xtick_location;
                line2.Y1 = ymin;
                line2.X2 = xtick_location;
                line2.Y2 = ymin + (ymax - ymin) * CanvasParameters.Minior_Tick_Length;
                line2.StrokeThickness = CanvasParameters.LineTickness;
                line2.Stroke = Brushes.Black;
                MainWindow.WindowInstance.canGraph.Children.Add(line2);
            }

            // Y axis
            double YTicks_OrderOfMagnitude = Math.Floor(Math.Log10(Data_y_max - Data_y_min)); // Get order of magnitude
            double YTicks_Major_tick_period = Math.Pow(10, YTicks_OrderOfMagnitude);
            double ystart = Math.Floor(Data_y_min / YTicks_Major_tick_period);
            int YTicks_Number_Of_Major_Ticks = (int)Math.Ceiling((Data_y_max - Data_y_min) / YTicks_Major_tick_period);
            string DecimalsZeros = "";
            if (YTicks_OrderOfMagnitude <= -1)
            {
                for (double i = YTicks_OrderOfMagnitude; i < 0; i++)
                {
                    DecimalsZeros += "#";
                }
            }
            for (int i = 0 ; i < YTicks_Number_Of_Major_Ticks; i++)
            {
                double ytick_location = -y_factor * ( (i + ystart) * YTicks_Major_tick_period - (Data_y_min - y_min_axis_pad)) + ymax; // y_factor * ( i* YTicks_Major_tick_period - (Data_y_min - y_min_axis_pad)) + ymin;
                if (ymin > ytick_location) { continue; }
                Line line = new Line();
                line.X1 = xmin;
                line.Y1 = ytick_location;
                line.X2 = xmin + (xmax - xmin) * CanvasParameters.Major_Tick_Length;
                line.Y2 = ytick_location;
                line.StrokeThickness = CanvasParameters.LineTickness;
                line.Stroke = Brushes.Black;
                MainWindow.WindowInstance.canGraph.Children.Add(line);
                Line line2 = new Line();
                line2.X1 = xmax;
                line2.Y1 = ytick_location;
                line2.X2 = xmax - (xmax - xmin) * CanvasParameters.Major_Tick_Length;
                line2.Y2 = ytick_location;
                line2.StrokeThickness = CanvasParameters.LineTickness;
                line2.Stroke = Brushes.Black;
                MainWindow.WindowInstance.canGraph.Children.Add(line2);

                Y_tick_Labels[i] = new TextBlock();
                if (YTicks_OrderOfMagnitude > -1)
                {
                    Y_tick_Labels[i].Text = ((long)( (i+ ystart) * YTicks_Major_tick_period)).ToString("D"); 
                } else
                {
                    Y_tick_Labels[i].Text =  ((double)( (i+ ystart) * YTicks_Major_tick_period)).ToString("0." + DecimalsZeros);
                }
                Y_tick_Labels[i].FontSize = CanvasParameters.Label_Size;
                Y_tick_Labels[i].Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                MainWindow.WindowInstance.canGraph.Children.Add(Y_tick_Labels[i]);
                Canvas.SetLeft(Y_tick_Labels[i], xmin);
                Canvas.SetTop(Y_tick_Labels[i], ytick_location - Y_tick_Labels[i].ActualHeight/2.0);
            }

        }
    }
}
