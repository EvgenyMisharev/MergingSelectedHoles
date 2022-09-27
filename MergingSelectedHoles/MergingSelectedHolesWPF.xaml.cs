using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MergingSelectedHoles
{

    public partial class MergingSelectedHolesWPF : Window
    {
        public string RoundHolesPositionButtonName;
        public double RoundHoleSizesUpIncrement;
        public double RoundHolePositionIncrement;

        MergingSelectedHolesSettings MergingSelectedHolesSettingsItem;
        public MergingSelectedHolesWPF()
        {
            MergingSelectedHolesSettingsItem = new MergingSelectedHolesSettings().GetSettings();
            InitializeComponent();
            SetSavedSettingsValueToForm();
        }
        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.DialogResult = true;
            this.Close();
        }

        private void radioButton_RoundHolesPosition_Checked(object sender, RoutedEventArgs e)
        {
            RoundHolesPositionButtonName = (this.groupBox_RoundHolesPosition.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            if (RoundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
            {
                label_RoundHolePosition.IsEnabled = true;
                textBox_RoundHolePositionIncrement.IsEnabled = true;
                label_RoundHolePositionMM.IsEnabled = true;
            }
            else if (RoundHolesPositionButtonName == "radioButton_RoundHolesPositionNo")
            {
                label_RoundHolePosition.IsEnabled = false;
                textBox_RoundHolePositionIncrement.IsEnabled = false;
                label_RoundHolePositionMM.IsEnabled = false;
            }
        }
        private void MergingSelectedHolesWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();
                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void SaveSettings()
        {
            MergingSelectedHolesSettingsItem = new MergingSelectedHolesSettings();

            MergingSelectedHolesSettingsItem.RoundHolesPositionButtonName = RoundHolesPositionButtonName;

            double.TryParse(textBox_RoundHoleSizesUpIncrement.Text, out RoundHoleSizesUpIncrement);
            MergingSelectedHolesSettingsItem.RoundHoleSizesUpIncrementValue = textBox_RoundHoleSizesUpIncrement.Text;

            double.TryParse(textBox_RoundHolePositionIncrement.Text, out RoundHolePositionIncrement);
            MergingSelectedHolesSettingsItem.RoundHolePositionIncrementValue = textBox_RoundHolePositionIncrement.Text;

            MergingSelectedHolesSettingsItem.SaveSettings();
        }
        private void SetSavedSettingsValueToForm()
        {
            if (MergingSelectedHolesSettingsItem.RoundHolesPositionButtonName != null)
            {
                if (MergingSelectedHolesSettingsItem.RoundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                {
                    radioButton_RoundHolesPositionYes.IsChecked = true;
                }
                else
                {
                    radioButton_RoundHolesPositionNo.IsChecked = true;
                }
            }

            if (MergingSelectedHolesSettingsItem.RoundHoleSizesUpIncrementValue != null)
            {
                textBox_RoundHoleSizesUpIncrement.Text = MergingSelectedHolesSettingsItem.RoundHoleSizesUpIncrementValue;
            }
            else
            {
                textBox_RoundHoleSizesUpIncrement.Text = "50";
            }

            if (MergingSelectedHolesSettingsItem.RoundHolePositionIncrementValue != null)
            {
                textBox_RoundHolePositionIncrement.Text = MergingSelectedHolesSettingsItem.RoundHolePositionIncrementValue;
            }
            else
            {
                textBox_RoundHolePositionIncrement.Text = "10";
            }
        }
    }
}
