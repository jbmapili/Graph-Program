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
        public int count=0, compTemp, totalCount=20, point=0;
        public double currentTemp, nextTemp, currentTime = 0, decimalValue, wholeValue;
        public bool firstStep = true;
        const int TEM_POS = 1500;
        const int MST_POS = 1550;
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
                dataGridView1.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.BackColor;
                dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;
                ReadValues();
                totalCount = data.Count;
                dataGridView1.DataSource = data;
                for (; count < totalCount; count++)
                {
                    if (Convert.ToDouble(dataGridView1.Rows[count].Cells[2].Value) != 0)
                    {
                        //first value
                        currentTemp = Convert.ToDouble(dataGridView1.Rows[count].Cells[1].Value);
                        point_Plot("Start");

                        //plot whole the next step
                        string unit = dataGridView1.Rows[count].Cells[3].Value.ToString();
                        currentTime = currentTime + (toHour(Convert.ToDouble(dataGridView1.Rows[count].Cells[2].Value), unit.Equals("秒") ? 3600 : unit.Equals("分") ? 60 : 1));
                        point_Plot("End");

                        //search for next value
                        count++;
                        for (; count < totalCount; count++)
                        {
                            if (Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value) != 0)
                            {
                                nextTemp = Convert.ToDouble(dataGridView1.Rows[count].Cells[1].Value);
                                compTemp = (currentTemp > nextTemp) ? -10 : 5;
                                count--;
                                break;
                            }
                        }

                        //perform calculation                    
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
                                        double add_Not_Enough = 0.2;
                                        add_Whole(add_Not_Enough);
                                    }
                                    currentTemp -= 1;
                                    nextTemp = nextTemp + decimalValue;
                                    currentTemp = (decimalValue != 0) ? currentTemp + 0.1 : currentTemp;
                                    for (; currentTemp < nextTemp; currentTemp = currentTemp + 0.1)
                                    {
                                        double add_Not_Enough = 0.02;
                                        add_Whole(add_Not_Enough);
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
                                        double add_Not_Enough = 0.1;
                                        add_Whole(add_Not_Enough);
                                    }
                                    currentTemp += 1;
                                    nextTemp = nextTemp + decimalValue;
                                    currentTemp = (decimalValue != 0) ? currentTemp - 0.1 : currentTemp;
                                    for (; currentTemp > nextTemp; currentTemp = currentTemp - 0.1)
                                    {
                                        double add_Not_Enough = 0.01;
                                        add_Whole(add_Not_Enough);
                                    }
                                }
                                currentTime += 0.01;
                                break;
                            }
                            else
                            {
                                chart1.Series[lineName].Points.AddXY(currentTime, currentTemp);
                                currentTemp = currentTemp + compTemp;
                                currentTime = currentTime + 1;
                                point++;
                                continue;
                            }
                        }
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

        private void add_Whole(double addValue)
        {
            currentTime += addValue;
            chart1.Series[lineName].Points.AddXY(currentTime, currentTemp);
            point++;
        }

        private void point_Plot(string startEnd)
        {
            chart1.Series[lineName].Points.AddXY(currentTime, currentTemp);
            chart1.Series[lineName].Points[point].Label = string.Format("{0}-{1}", count + 1, startEnd);
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
            return Math.Round(valTime / valDiv,2);
        }
        private void ReadValues()
        {
            for (int step = 0; step < totalCount; step++)
            {
                object[] oValueArray;
                short[] wQualityArray;
                OpcRcw.Da.FILETIME[] fTimeArray;
                int[] nErrorArray;
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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            count = point = 0;
            currentTime = currentTemp = 0;
            data.Clear();
            chart1.Series[lineName].Points.Clear();
            Initialize();
//            dataGridView1.SelectAll();
            dataGridView1.Refresh();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            PrintingManager printManager = chart1.Printing;
            chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Top 
                = chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Left 
                = chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Bottom  
                = chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Right 
                = 10;
            printManager.PrintDocument.DefaultPageSettings.Landscape = true;
            chart1.Printing.PrintPreview();
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
