using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using OpcRcw.Da;

namespace PlotCharts
{
    public partial class PlotCharts : Form
    {
        DxpSimpleAPI.DxpSimpleClass opc = new DxpSimpleAPI.DxpSimpleClass();
        public int count=-1, compTemp, totalCount=20, point=0;
        public double currentTemp=0, nextTemp, currentTime = 0, decimalValue, wholeValue;
        public bool firstStep = true;
        const int TEM_POS = 1500;
        const int MST_POS = 1550;
        const int INITIAL_POS = 1909;
        const int LOWER_TIME_POS = 1600;
        const int UNIT_BIT_POS = 1500;
        const int STEP_OFFSET = 50;
        const double minValue = -100.0;
        const double maxValue = 100.0;
        const string DEV_NAME = "DEV1";
        const string WORD_REG_PREFIX = "D";
        const string BIT_REG_PREFIX = "M";
        const int MAX_DIGIT = 10000;

        readonly string lineName = "温度"; //"Temperature";

        List<DataCollection> data = new List<DataCollection> { };
        public PlotCharts()
        {
            InitializeComponent(); 

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            if (opc.Connect("localhost", "Takebishi.dxp"))
            {
                chart1.ChartAreas[0].AxisY.ScaleView.Zoom(0, 10);
                count = -1;
                ReadValues();
                nextTemp = curnextTemp(0);
                compTemp = (currentTemp > nextTemp) ? -10 : 5;
                point_Plot("Start");
                perform_Calc();
                for (count = 0; count < totalCount; count++)
                {
                    if (curnextTime(count) != 0)
                    {
                        //first value
                        currentTemp = curnextTemp(count);
                        Debug.WriteLine("");
                        point_Plot("Start");

                        //plot whole the next step
                        string unit = dataGridView1.Rows[count].Cells[3].Value.ToString();
                        currentTime = currentTime + (toHour(curnextTime(count), unit.Equals("秒") ? 3600 : unit.Equals("分") ? 60 : 1));
                        point_Plot("End");

                        //search for next value
                        count++;
                        for (; count < totalCount; count++)
                        {
                            if (curnextTime(count) != 0)
                            {
                                nextTemp = curnextTemp(count);
                                compTemp = (currentTemp > nextTemp) ? -10 : 5;
                                count--;
                                break;
                            }
                        }

                        //perform calculation                    
                        perform_Calc();
                    }
                    continue;
                }
                chart1.ChartAreas[0].AxisX.Maximum = Math.Ceiling(currentTime);

                //double min = data.Min(x => x.温度);
                //double max = data.Max(x => x.温度);
                //chart1.ChartAreas[0].AxisY.ScaleView.Zoom(min, max);

                //double offset = (max - min) * 0.1;
                //chart1.ChartAreas[0].AxisY.ScaleView.Zoom(min-offset, max+offset);
                chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();
            }
        }

        private double curnextTime(int count)
        {
            return Convert.ToDouble(dataGridView1.Rows[count].Cells[2].Value);
        }

        private double curnextTemp(int count)
        {
            return Convert.ToDouble(dataGridView1.Rows[count].Cells[1].Value);
        }

        private void perform_Calc()
        {
            for (; currentTemp != nextTemp; )
            {
                if (currentTemp >= nextTemp && compTemp == 5)
                {
                    if (currentTemp != nextTemp)
                    {
                        not_Enough();
                        currentTemp += 1;
                        for (; currentTemp <= nextTemp; currentTemp = currentTemp + 1)
                        {
                            add_Whole(0.2);
                        }
                        currentTemp -= 1;
                        nextTemp = nextTemp + decimalValue;
                        currentTemp = (decimalValue != 0) ? currentTemp + 0.1 : currentTemp;
                        for (; currentTemp < nextTemp; currentTemp = currentTemp + 0.1)
                        {
                            add_Whole(0.02);
                        }
                    }
                    currentTime += 0.02;
                    break;
                }
                else if (currentTemp <= nextTemp && compTemp == -10)
                {
                    if (currentTemp != nextTemp)
                    {
                        not_Enough();
                        currentTemp -= 1;
                        for (; currentTemp >= nextTemp; currentTemp = currentTemp - 1)
                        {
                            add_Whole(0.1);
                        }
                        currentTemp += 1;
                        nextTemp = nextTemp + decimalValue;
                        currentTemp = (decimalValue != 0) ? currentTemp - 0.1 : currentTemp;
                        for (; currentTemp > nextTemp; currentTemp = currentTemp - 0.1)
                        {
                            add_Whole(0.01);
                        }
                    }
                    currentTime += 0.01;
                    break;
                }
                else
                {
                    plot();
                    currentTemp = currentTemp + compTemp;
                    currentTime = currentTime + 1;
                    point++;
                    continue;
                }
            }
        }

        private void add_Whole(double addValue)
        {
            currentTime += addValue;
            plot();
            point++;
        }

        private void plot()
        {
            chart1.Series[lineName].Points.AddXY(currentTime, currentTemp);
        }

        private void point_Plot(string startEnd)
        {
            plot();
            chart1.Series[lineName].Points[point].Label = string.Format("{0}-{1}", count>=0?count + 1:0, startEnd);
            point++;
        }

        private void not_Enough()
        {
            currentTemp = currentTemp - compTemp;
            decimalValue = Math.Round((nextTemp - currentTemp) % 1, 1);
            wholeValue = Convert.ToInt32((nextTemp - currentTemp) / 1);
            wholeValue = ((wholeValue + decimalValue) == (nextTemp - currentTemp)) ? wholeValue : wholeValue - 1;
            nextTemp = nextTemp - decimalValue;
            currentTime -= 1;
        }


        private double toHour(double valTime, int valDiv)
        {
            return Math.Round(valTime / valDiv,4);
        }
        private void ReadValues()
        {
            object[] oValueArray;
            short[] wQualityArray;
            OpcRcw.Da.FILETIME[] fTimeArray;
            int[] nErrorArray;
            for (int step = 0; step < totalCount; step++)
            {
                string[] target = { 
                                      DEV_NAME + "." + WORD_REG_PREFIX + "" + (TEM_POS + (step + 1)), // get temp values
                                      DEV_NAME + "." + WORD_REG_PREFIX + "" + (LOWER_TIME_POS + (step + 1)), // get time
                                      DEV_NAME + "." + WORD_REG_PREFIX + "" + (LOWER_TIME_POS + STEP_OFFSET + (step + 1)), // get time
                                      DEV_NAME + "." + BIT_REG_PREFIX + "" + (UNIT_BIT_POS + (step + 1)), // get unit seconds
                                      DEV_NAME + "." + BIT_REG_PREFIX + "" + (UNIT_BIT_POS + STEP_OFFSET + (step + 1)), // get unit minutes
                                      DEV_NAME + "." + BIT_REG_PREFIX + "" + (UNIT_BIT_POS + (STEP_OFFSET * 2) + (step + 1)), // get unit hours
                                  };
                try 
                { 
                    if (opc.Read(target, out oValueArray, out wQualityArray, out fTimeArray, out nErrorArray))
                    {
                        double rawValueTemp = Convert.ToDouble(oValueArray[0]);
                        double newVal = minValue + ((maxValue - minValue) * (rawValueTemp - 0)) / (MAX_DIGIT - 0);
                        data.Add(new DataCollection { 
                                                         ステップ = step + 1, 
                                                         温度 = newVal != -100 ? Math.Round(newVal,1) : 0, 
                                                         時間 = Convert.ToInt32(oValueArray[1]) + (Convert.ToInt32(oValueArray[2]) * 10000),
                                                         単位 = Convert.ToInt32(oValueArray[3]) == 1 ? "秒" : Convert.ToInt32(oValueArray[4]) == 1 ? "分" : "時間"
                                                    }
                                );

                    }
                }
                catch (Exception) { }

            }
            string[] targetInitial = { DEV_NAME + "." + WORD_REG_PREFIX + "" + (INITIAL_POS) };
            try
            {
                if (opc.Read(targetInitial, out oValueArray, out wQualityArray, out fTimeArray, out nErrorArray))
                {
                    double rawValueTemp = Convert.ToDouble(oValueArray[0]);
                    double newVal = minValue + ((maxValue - minValue) * (rawValueTemp - 0)) / (MAX_DIGIT - 0);
                    currentTemp = newVal != -100 ? Math.Round(newVal, 1) : 0;

                }
            }
            catch (Exception) { }
            dataGridView1.DataSource = data;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            point = 0;
            currentTime = currentTemp = 0;
            data.Clear();
            chart1.Series[lineName].Points.Clear();
            Initialize();
            dataGridView1.Refresh();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            PrintingManager printManager = chart1.Printing;
            printManager.PrintDocument.DefaultPageSettings.Margins.Top
                = printManager.PrintDocument.DefaultPageSettings.Margins.Left
                = printManager.PrintDocument.DefaultPageSettings.Margins.Bottom
                = printManager.PrintDocument.DefaultPageSettings.Margins.Right 
                = 10;
            printManager.PrintDocument.DefaultPageSettings.Landscape = true;
            printManager.PrintPreview();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Png Image|*.png";
            save.Title = "Save the File";
            if (save.ShowDialog() == DialogResult.OK)
            {
                string fName = save.FileName;
                this.chart1.SaveImage(fName, ChartImageFormat.Png);
            }
        }
    }
}
