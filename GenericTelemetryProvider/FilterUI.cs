﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GenericTelemetryProvider
{
    public partial class FilterUI : Form
    {

        public static FilterUI Instance;

        public GenericProviderData.DataKey filterKey = GenericProviderData.DataKey.Max;
        Series filteredSeries;
        Series rawSeries;


        public FilterUI()
        {
            Instance = this;

            InitializeComponent();
        }

        private void FilterUI_Load(Object sender, EventArgs e)
        { 
            filterChart.Series.Clear();

            var chart = filterChart.ChartAreas[0];

            chart.AxisX.IntervalType = DateTimeIntervalType.Number;
            chart.AxisX.LabelStyle.Format = "";
            chart.AxisY.LabelStyle.Format = "";
            chart.AxisY.LabelStyle.IsEndLabelVisible = true;

            filteredSeries = filterChart.Series.Add("filtered");
            filteredSeries.ChartType = SeriesChartType.Line;
            filteredSeries.Color = Color.Red;
            filteredSeries.IsVisibleInLegend = true;

            filteredSeries.Points.Clear();

            rawSeries = filterChart.Series.Add("raw");
            rawSeries.ChartType = SeriesChartType.Line;
            rawSeries.Color = Color.Blue;
            rawSeries.IsVisibleInLegend = true;

            rawSeries.Points.Clear();

            for(int key = 0; key < (int)GenericProviderData.DataKey.Max; ++key)
            {
                keyComboBox.Items.Add(((GenericProviderData.DataKey)key).ToString());
            }

            keyComboBox.SelectedIndex = 0;

            InitChartForKey((GenericProviderData.DataKey)keyComboBox.SelectedIndex);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000 / 100;
            timer.Tick += new EventHandler(RefreshChart);
            timer.Start();
        }

        private void filterChart_Click(object sender, EventArgs e)
        {

        }

        public void InitChartForKey(GenericProviderData.DataKey dataKey)
        {
            filterKey = dataKey;
            
            filterChart.Invoke((Action)delegate
            {
                var chart = filterChart.ChartAreas[0];

                chart.AxisX.Minimum = 0;
                chart.AxisX.Maximum = 1;
                chart.AxisY.Minimum = -10;
                chart.AxisY.Maximum = 10;
                chart.AxisX.Interval = 0;
                chart.AxisY.Interval = 0;
            });

            flowLayoutFilters.Invoke((Action)delegate
            {

                flowLayoutFilters.Controls.Clear();

                List<FilterBase> filters = FilterModule.Instance.filters[(int)filterKey];

                if (filters != null)
                {
                    foreach (FilterBase filter in filters)
                    {
                        if (filter is NestedSmoothFilter)
                        {
                            SmoothFilterControl newControl = new SmoothFilterControl();
                            newControl.SetFilter((NestedSmoothFilter)filter);

                            flowLayoutFilters.Controls.Add(newControl);
                        }
                        else
                        if (filter is KalmanNoiseFilter)
                        {
                            KalmanFilterControl newControl = new KalmanFilterControl();
                            newControl.SetFilter((KalmanNoiseFilter)filter);

                            flowLayoutFilters.Controls.Add(newControl);
                        }
                    }
                }

                Button addButton = new Button();
                addButton.Text = @"ADD FILTER";
                addButton.Click += new System.EventHandler(this.AddButtonClick);

                flowLayoutFilters.Controls.Add(addButton);
            });

        }
        public void AddButtonClick(object sender, EventArgs e)
        {
            FilterPicker picker = new FilterPicker();

            Thread x = new Thread(new ParameterizedThreadStart((form) =>
            {
                ((FilterPicker)form).ShowDialog();
            }));
            x.Start(picker);

        }


        public void RefreshChart(object sender, EventArgs e)
        {
            if (filterKey == GenericProviderData.DataKey.Max)
                return;

            filterChart.Invoke((Action)delegate
            {
                var chart = filterChart.ChartAreas[0];
                filteredSeries.Points.Clear();
                rawSeries.Points.Clear();

                List<GenericProviderData> filteredData;
                FilterModule.Instance.GetFilteredHistory(out filteredData);
                List<GenericProviderData> rawData;
                FilterModule.Instance.GetRawHistory(out rawData);

                chart.AxisX.Maximum = filteredData.Count();

                for (int i = 0; i < filteredData.Count; ++i)
                {
                    GenericProviderData data = filteredData[i];

                    filteredSeries.Points.AddXY(i, data.data[(int)filterKey]);
                }

                for (int i = 0; i < rawData.Count; ++i)
                {
                    GenericProviderData data = rawData[i];

                    rawSeries.Points.AddXY(i, data.data[(int)filterKey]);
                }

            });

        }

        private void keyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitChartForKey((GenericProviderData.DataKey)keyComboBox.SelectedIndex);
        }

        public void DeleteControl(UserControl control)
        {
            int index = flowLayoutFilters.Controls.IndexOf(control);

            if (control is SmoothFilterControl)
            {
                FilterModule.Instance.DeleteFilter(((SmoothFilterControl)control).filter, filterKey);
            }
            else
            if (control is KalmanFilterControl)
            {
                FilterModule.Instance.DeleteFilter(((KalmanFilterControl)control).filter, filterKey);
            }

            flowLayoutFilters.Controls.Remove(control);
        }

        public void MoveControl(UserControl control, int direction)
        {
            int index = flowLayoutFilters.Controls.GetChildIndex(control);

            index = Math.Min(flowLayoutFilters.Controls.Count-2, Math.Max(0, index + direction));

            flowLayoutFilters.Controls.SetChildIndex(control, index);

            if (control is SmoothFilterControl)
            {
                FilterModule.Instance.MoveFilter(((SmoothFilterControl)control).filter, filterKey, direction);
            }
            else
            if (control is KalmanFilterControl)
            {
                FilterModule.Instance.MoveFilter(((KalmanFilterControl)control).filter, filterKey, direction);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            FilterModule.Instance.SaveConfig();
        }
    }
}