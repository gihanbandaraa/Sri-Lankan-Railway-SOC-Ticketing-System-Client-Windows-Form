using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Net;
using System.Net.Http;

namespace TrainBookingSystem
{
    public partial class Register : Form
    {
        public Register()
        {
            InitializeComponent();

        }

        int Id;
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login login = new Login();
            login.Show();
            this.Hide();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrWhiteSpace(txbUserName.Text) ||
              string.IsNullOrWhiteSpace(txbPassword.Text) ||
              string.IsNullOrWhiteSpace(txbConfirmPassword.Text) ||
              string.IsNullOrWhiteSpace(txbNIC.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (txbPassword.Text != txbConfirmPassword.Text)
            {
                MessageBox.Show("Password and confirm password do not match.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string nic = txbNIC.Text;
            if (IsNICAlreadyRegistered(nic))
            {
                MessageBox.Show("NIC already exists. Please choose a different NIC.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            string url = "https://localhost:7125/api/User";
            HttpClient client = new HttpClient();
            Users users = new Users(); 
            
            users.UserName=txbUserName.Text;
            users.Password=txbPassword.Text;
            users.NIC=txbNIC.Text;

            string data = (new JavaScriptSerializer()).Serialize(users);
            var content = new StringContent(data, UnicodeEncoding.UTF8, "application/json");

            var res = client.PostAsync(url, content).Result;
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show("User Added Successfully");

                txbUserName.Clear();
                txbPassword.Clear();
                txbConfirmPassword.Clear();
                txbNIC.Clear();
            }
            else
            {
                MessageBox.Show("Fail to add User");
            }
        }
        private bool IsNICAlreadyRegistered(string nic)
        {
            string apiUrl = "https://localhost:7125/api/User/IsNICRegistered/" + nic;

            using (var client = new HttpClient())
            {
                var response = client.GetAsync(apiUrl).Result;

                if (response.IsSuccessStatusCode)
                {
     
                    string result = response.Content.ReadAsStringAsync().Result;
                    return bool.Parse(result); 
                }
                else
                {
    
                    MessageBox.Show("Failed to check NIC registration status.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

    }
    public class Users
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string NIC { get; set; }
    }
}
