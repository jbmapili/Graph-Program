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
        public int spentTime=0, count=0, compTemp, totalCount=20;
        public double currentTemp, nextTemp, currentTime = 0, decimalValue, wholeValue;
        public bool firstStep = true;
        const int TEM_POS = 1500;
        const int MST_POS = 1550;
        const int LOWER_TIME_POS = 1600;
        const int UNIT_BIT_POS = 1500;
        const int STEP_OFFSET = 50;
        const int PAUSE_BIT_POS = 1700;
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
            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(0, 1);
            chart1.ChartAreas[0].AxisY.ScaleView.Zoom(0, 15);
            if(opc.Connect("localhost", "Takebishi.dxp"))
            {
                    ReadValues();
                    totalCount = data.Count;
                    dataGridView1.DataSource = data;
                    for (; count < totalCount ; count++) 
                    {
                        Debug.WriteLine("Continue" + count);
                        if (Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value) != 0)
                        {
                            //first value
                            currentTemp = Convert.ToDouble(dataGridView1.Rows[count].Cells[1].Value);
                            chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                            Debug.WriteLine("First Coordinates: " + currentTime + "," + currentTemp);

                            //plot whole the next step
                            string unit = dataGridView1.Rows[count].Cells[3].Value.ToString();
                            if (("second").Equals(unit))
                            {
                                currentTime = currentTime + Math.Round(secToHour(Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value)),4);
                            }
                            else if (("minute").Equals(unit))
                            {
                                currentTime = currentTime + Math.Round(minToHour(Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value)), 4);
                            }
                            else
                            {
                                currentTime = currentTime + Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value);
                            }
                            chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                            Debug.WriteLine(" Next Coordinates: " + currentTime + "," + currentTemp);

                            //search for next value
                            count++;
                            for(; count < totalCount; count++){
                                if(Convert.ToInt32(dataGridView1.Rows[count].Cells[2].Value) != 0){
                                    nextTemp=Convert.ToDouble(dataGridView1.Rows[count].Cells[1].Value);
                                    compTemp = (currentTemp > nextTemp) ? -10 : 5;
                                    count--;
                                    break;
                                }
                            }

                            //perform calculation                    
                            Debug.WriteLine("\nPeform Calculation:\nCurrent Temp: " + currentTemp +"\n Next Temp: "+ nextTemp);
                            for (; currentTemp != nextTemp; )
                            {
                                if (currentTemp >= nextTemp && compTemp == 5)
                                {
                                    if (currentTemp != nextTemp) {
                                        currentTemp = currentTemp - compTemp;
                                        decimalValue = (nextTemp - currentTemp) % 1;
                                        wholeValue = Convert.ToInt32((nextTemp - currentTemp) / 1);
                                        wholeValue=((wholeValue + decimalValue) == (nextTemp - currentTemp)) ? wholeValue : wholeValue-1;
                                        nextTemp = nextTemp - decimalValue;
                                        currentTime -= 1;
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
                                        currentTemp = currentTemp - compTemp;
                                        decimalValue = Math.Round((nextTemp - currentTemp) % 1,1);
                                        wholeValue = Convert.ToInt32((nextTemp - currentTemp) / 1);
                                        wholeValue = ((wholeValue + decimalValue) == (nextTemp - currentTemp)) ? wholeValue : wholeValue - 1;
                                        nextTemp = nextTemp - decimalValue;
                                        currentTime -= 1;
                                        currentTemp -= 1;
                                        for (; currentTemp >= nextTemp; currentTemp = currentTemp - 1)
                                        {
                                            currentTime += 0.1;
                                            chart1.Series["Temperature"].Points.AddXY(currentTime, currentTemp);
                                            Debug.WriteLine("Coordinates: (" + currentTime + ", " + currentTemp + ")");
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
            }
        }


        private double secToHour(double sec)
        {
            sec = sec / 3600;
            Debug.WriteLine("Converted value sec: " + sec);
            return sec;
        }
        private double minToHour(double min)
        {
            min = min / 60;
            Debug.WriteLine("Converted value min: " + min);
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
                                      DEV_NAME + "." + BIT_REG_PREFIX + "" + (PAUSE_BIT_POS + (step + 1)), // get pause value
                                  };
                if (opc.Read(target, out oValueArray, out wQualityArray, out fTimeArray, out nErrorArray))
                {
                    Debug.WriteLine("Read Succeed");
                    double rawValueTemp = Convert.ToDouble(oValueArray[0]);
                    double newVal = minValue + ((maxValue - minValue) * (rawValueTemp - 0)) / (MAX_DIGIT - 0);
                    data.Add(new DataCollection { 
                                                     Step = step + 1, 
                                                     Temperature = newVal != -100 ? Math.Round(newVal,1) : 0, 
                                                     Time = Convert.ToInt32(oValueArray[1]) + (Convert.ToInt32(oValueArray[2]) * 10000),
                                                     Unit = Convert.ToInt32(oValueArray[3]) == 1 ? "second" : Convert.ToInt32(oValueArray[4]) == 1 ? "minute" : "hour",
                                                     Pause = Convert.ToInt32(oValueArray[6]) == 1 ? "Yes" : "No"
                                                }
                            );
                }
                else
                {
                    Debug.WriteLine("Read Error");
                }
            }
        }
    }
}
