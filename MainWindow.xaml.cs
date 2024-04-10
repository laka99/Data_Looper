using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows;

namespace DataLooper
{
    /// <summary>
    /// interaction logic for mainwindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string jsonconfig = ConfigurationManager.AppSettings["json"];
        public MainWindow()
        {
            InitializeComponent();
            while (true)
            {
                dbloop();
                Thread.Sleep(5000);
            }
        }

        SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["conString"].ConnectionString);
        //SqlConnection con = new SqlConnection(@"data source =.;initial catalog=mycarparklatest;integrated security=true");

        public void dbloop()
        {
            try
            {
                SqlCommand cmd = new SqlCommand("select top 1 vehicleimage,vehicleplateimage,platenumber,id,vehicleStatus,vehicleTime from tbl_datalooper where isUsed = 0 ", con);
                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();
                JObject objectedstring = JObject.Parse(jsonconfig);
                int id = 0;
                char[] charsToTrim = { '[', ']', '"' };
                while (sdr.Read())
                {
                    id = int.Parse(sdr["id"].ToString());
                    string vehiclestatus = sdr["vehicleStatus"].ToString();
                    if (objectedstring != null)
                    {
                        // Get the offset from current time in UTC time
                        DateTimeOffset dto = new DateTimeOffset(DateTime.Parse(sdr["vehicleTime"].ToString()));
                        // Get the unix timestamp in seconds
                        string unixTime = dto.ToUnixTimeSeconds().ToString();
                        // Get the unix timestamp in seconds, and add the milliseconds
                        string unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();
                        objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["o_overview_image"]["o_encoded_image_buffer"]["o_elements"][0]["s_frame"] = sdr["vehicleimage"].ToString().Trim(charsToTrim);
                        objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["o_plate_image"]["o_encoded_image_buffer"]["o_elements"][0]["s_frame"] = sdr["vehicleplateimage"].ToString().Trim(charsToTrim);
                        objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["s_registration"] = sdr["platenumber"].ToString();
                        objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["n_capture_date_time"] = unixTimeMilliSeconds;
                        objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["n_camera_capture_timestamp_milliseconds"] = unixTimeMilliSeconds;
                        if (vehiclestatus == "In")
                        {
                            objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["n_d_code"] = 1;
                        }else
                        {
                            objectedstring["o_jobject"]["o_result_list"]["o_elements"][0]["n_d_code"] = 2;
                        }
                    }
                }
                sdr.Close();
                SendEntyExits(objectedstring, id);
                con.Close();    
            }
            catch (Exception ex)
            {
                Console.WriteLine("error while fetching data from the databse exception ---> " + ex.ToString());
            }
            finally
            {
                if(con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        private void SendEntyExits(JObject vehicleDataObject, int id)
        {
            try
            {
                HttpClient client = new HttpClient();
                //convert jobject to string
                string stringObject =  JsonConvert.SerializeObject(vehicleDataObject); 

                //convert object into data content to send to API
                HttpContent _vehDataContent = new StringContent(stringObject, Encoding.UTF8, "application/json");

                string apiUrl = "http://192.168.1.111/mycarparkvReciever/api/Anpr/PostAnpr";
                client.DefaultRequestHeaders.Add("X-Api-Key", "pgH7QzFHJx4w46fI~5Uzi4RvtTwlEXp");
                HttpResponseMessage response = client.PostAsync(apiUrl, _vehDataContent).Result;
                if(con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
                //con.Open();
                if (response.IsSuccessStatusCode)
                {
                    string responseString = response.Content.ReadAsStringAsync().Result;
                    SqlCommand cmd = new SqlCommand("update tbl_datalooper set isUsed = 1 where id = @id", con);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    if(con.State == ConnectionState.Open)
                    {
                        con.Close();
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

    }
}
