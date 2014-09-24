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
    public partial class Form1 : Form
    {

        DxpSimpleAPI.DxpSimpleClass opc = new DxpSimpleAPI.DxpSimpleClass();
        public int count=0, compTemp, totalCount=20;
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
        List<DataCollection> data = new List<DataCollection> { };
        public Form1()
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
                chart1.ChartAreas[0].AxisX.ScaleView.Zoom(0, 10);
                chart1.ChartAreas[0].AxisY.ScaleView.Zoom(0, 15);
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
                        chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);

                        //plot whole the next step
                        string unit = dataGridView1.Rows[count].Cells[3].Value.ToString();
                        if (("second").Equals(unit))
                        {
                            currentTime = currentTime + secToHour(Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value));
                        }
                        else if (("minute").Equals(unit))
                        {
                            currentTime = currentTime + minToHour(Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value));
                        }
                        else
                        {
                            currentTime = currentTime + Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value);
                        }
                        chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);

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
                                        currentTime += 0.2;
                                        chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                                    }
                                    currentTemp -= 1;
                                    nextTemp = nextTemp + decimalValue;
                                    currentTemp = (decimalValue != 0) ? currentTemp + 0.1 : currentTemp;
                                    for (; currentTemp < nextTemp; currentTemp = currentTemp + 0.1)
                                    {
                                        currentTime += 0.02;
                                        chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
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
                                        currentTime += 0.1;
                                        chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                                    }
                                    currentTemp += 1;
                                    nextTemp = nextTemp + decimalValue;
                                    currentTemp = (decimalValue != 0) ? currentTemp - 0.1 : currentTemp;
                                    for (; currentTemp > nextTemp; currentTemp = currentTemp - 0.1)
                                    {
                                        currentTime += 0.01;
                                        chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                                    }
                                }
                                currentTime += 0.01;
                                break;
                            }
                            else
                            {
                                chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                                currentTemp = currentTemp + compTemp;
                                currentTime = currentTime + 1;
                                continue;
                            }
                        }
                    }
                    continue;
                }
                chart1.ChartAreas[0].AxisX.Maximum = Math.Ceiling(currentTime);
            }
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


        private double secToHour(double sec)
        {
            sec = Math.Round(sec / 3600,2);
            return sec;
        }
        private double minToHour(double min)
        {
            min = Math.Round(min / 60,2);
            return min;
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
                                                         Step = step + 1, 
                                                         Temperature = newVal != -100 ? Math.Round(newVal,1) : 0, 
                                                         Time = Convert.ToInt32(oValueArray[1]) + (Convert.ToInt32(oValueArray[2]) * 10000),
                                                         Unit = Convert.ToInt32(oValueArray[3]) == 1 ? "second" : Convert.ToInt32(oValueArray[4]) == 1 ? "minute" : "hour"
                                                    }
                                );
                    }
                }
                catch (Exception) { }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            count = 0;
            currentTime = currentTemp = 0;
            data.Clear();
            chart1.Series["Temperature"].Points.Clear();
            Initialize();
            dataGridView1.SelectAll();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PrintingManager printManager = chart1.Printing;
            chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Top = 
            chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Left = 
            chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Bottom = 
            chart1.Printing.PrintDocument.DefaultPageSettings.Margins.Right = 10;
            printManager.PrintDocument.DefaultPageSettings.Landscape = true;
            chart1.Printing.PrintPreview();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Png Image|*.png|JPeg Image|*.jpg|Bitmap Image|*.bmp";
            save.Title = "Save the File";
            if (save.ShowDialog() == DialogResult.OK)
            {
                string fName = save.FileName;
                this.chart1.SaveImage(fName, ChartImageFormat.Png);
            }

        }

        //private void button4_Click(object sender, EventArgs e)
        //{
            //if (btnZoom.Text.Equals("Fit Graph"))
            //{ 
            //    double max=0;
            //    double min=0;
            //    for (int minMax = 0; minMax < totalCount; minMax++)
            //    {
            //        if (Convert.ToDouble((dataGridView1.Rows[minMax].Cells[1].Value)) > max)
            //        {
            //            max = Convert.ToDouble((dataGridView1.Rows[minMax].Cells[1].Value));
            //        }
            //        else if (Convert.ToDouble((dataGridView1.Rows[minMax].Cells[1].Value)) < min)
            //        {
            //            min = Convert.ToDouble((dataGridView1.Rows[minMax].Cells[1].Value));
            //        }
            //    }
            //    chart1.ChartAreas[0].AxisY.Maximum = max;
            //    chart1.ChartAreas[0].AxisY.Minimum = min;
            //    chart1.ChartAreas[0].AxisY.ScaleView.Zoom(Math.Floor(min), Math.Ceiling(max));
            //    chart1.ChartAreas[0].AxisX.Maximum = Math.Ceiling(currentTime);
            //    chart1.ChartAreas[0].AxisX.ScaleView.Zoom(0, Math.Ceiling(currentTime)+1);
            //    btnZoom.Text = "Unfit Graph";
            //}
            //else
            //{
            //    chart1.ChartAreas[0].AxisY.ScaleView.Zoom(0, 15);
            //    chart1.ChartAreas[0].AxisX.ScaleView.Zoom(0, 10);
            //    btnZoom.Text = "Fit Graph";
            //}
         //}
    }
}
