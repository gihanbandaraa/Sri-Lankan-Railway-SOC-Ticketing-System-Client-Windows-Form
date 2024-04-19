using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using QRCoder;


namespace TrainBookingSystem
{
    public partial class Homepage : Form
    {
        private List<SeatButton> seatButtons = new List<SeatButton>();
        private HttpClient _httpClient;
        private MyTrain selectedTrain; // Store the selected train


        private const int numRows = 5;
        private const int numCols = 6;
        private const int buttonWidth = 60;
        private const int buttonHeight = 30;
        private const int startX = 50;
        private const int startY = 50;
        private const int gapX = 10;
        private const int gapY = 10;
        private const int maxSeatsToSelect = 5;

        private List<string> selectedSeats = new List<string>();

        public Homepage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();

        }

        private void Homepage_Load(object sender, EventArgs e)
        {
            LoadTrain();
        }
        private async Task<List<MyTrain>> FindTrainsAsync(string date, string startStation, string destinationStation)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:7125/api/Train/Search?date={date}&startStation={startStation}&destinationStation={destinationStation}");

            if (response.IsSuccessStatusCode)
            {
                string trainJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<MyTrain>>(trainJson);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                MessageBox.Show("No Matching Train.");
                return LoadTrain(); 
            }
            else
            {
                MessageBox.Show($"Failed to retrieve train data. Status code: {response.StatusCode}");
                return null;
            }
        }
        private async void btnFind_Click(object sender, EventArgs e)
        {
            string selectedDate = dtpDate.Value.ToString("yyyy-MM-dd");
            string startStation = comStartStation.SelectedItem?.ToString();
            string destinationStation = comDestinationStation.SelectedItem?.ToString();

            if (selectedDate != null && startStation != null && destinationStation != null)
            {
              
                List<MyTrain> trains = await FindTrainsAsync(selectedDate, startStation, destinationStation);

                dgvTrainData.DataSource = trains;
            }
            else
            {
                MessageBox.Show("Please select a date and stations.");
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            LoadTrain();
        }
        public List<MyTrain> LoadTrain()
        {
            string trainUrl = "https://localhost:7125/api/Train";
            string stationUrl = "https://localhost:7125/api/Train/Stations";

            // Create a list to hold the train data
            List<MyTrain> trains = new List<MyTrain>();

            // Download train data
            using (WebClient client = new WebClient())
            {
                client.Headers["content-type"] = "application/json";
                client.Encoding = Encoding.UTF8;

                string trainJson = client.DownloadString(trainUrl);
                // Deserialize the train data and add it to the list
                trains = JsonConvert.DeserializeObject<List<MyTrain>>(trainJson);
                dgvTrainData.DataSource = null;
                dgvTrainData.DataSource = trains;
            }

            comStartStation.Items.Clear();
            comDestinationStation.Items.Clear();
            // Download station data
            using (WebClient client = new WebClient())
            {
                client.Headers["content-type"] = "application/json";
                client.Encoding = Encoding.UTF8;

                string stationJson = client.DownloadString(stationUrl);
                var stationsDto = JsonConvert.DeserializeObject<StationsDTO>(stationJson);

                // Populate ComboBox controls with station data
                foreach (string station in stationsDto.StartStations)
                {
                    comStartStation.Items.Add(station);
                }

                foreach (string station in stationsDto.DestinationStations)
                {
                    comDestinationStation.Items.Add(station);
                }
            }

            // Return the list of trains
            return trains;
        }


        private async Task LoadSeats(int firstClassCapacity, int secondClassCapacity, int thirdClassCapacity)
        {
            string[] classNames = { "First Class", "Second Class", "Third Class" };
            int[] capacities = { firstClassCapacity, secondClassCapacity, thirdClassCapacity };

         
            seatPanel.Controls.Clear();
            btnConfirm.Visible = true;
            NIClable.Visible = true;
            txbNIC.Visible = true;

            List<string> bookedSeats = await GetBookedSeats(selectedTrain.TrainId.ToString(), selectedTrain.Date.ToString());

            int currentY = startY; 

            for (int classIndex = 0; classIndex < capacities.Length; classIndex++)
            {
                // Add label to indicate class
                Label classLabel = new Label();
                classLabel.Text = classNames[classIndex];
                classLabel.AutoSize = true;
                classLabel.Location = new Point(startX, currentY - 20);
                seatPanel.Controls.Add(classLabel);

                int totalRows = capacities[classIndex] / numCols;
                for (int row = 0; row < totalRows; row++)
                {
                    for (int col = 0; col < numCols; col++)
                    {
                        string seatId = $"{classNames[classIndex]}{(char)('A' + row)}{col + 1}";

                        SeatButton seatButton = new SeatButton(row, col, classNames[classIndex]);
                        seatButton.Text = $"{(char)('A' + row)}{col + 1}";
                        seatButton.Size = new Size(buttonWidth, buttonHeight);
                 
                        seatButton.Location = new Point(startX + col * (buttonWidth + gapX),
                                                        currentY + row * (buttonHeight + gapY));

                      
                        if (bookedSeats.Contains(seatId))
                        {
                            seatButton.BackColor = Color.Red; 
                            seatButton.Enabled = false; 
                        }

                        seatButton.Click += SeatButton_Click;
                        seatPanel.Controls.Add(seatButton); 
                        seatButtons.Add(seatButton);

                    
                    }
                }
                currentY += totalRows * (buttonHeight + gapY) + 30;
            }
        }

        private async Task<List<string>> GetBookedSeats(string trainId, string date)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:7125/api/Booking/BookedSeats?TrainId={trainId}&Date={date}");

                if (response.IsSuccessStatusCode)
                {
                    string bookedSeatsJson = await response.Content.ReadAsStringAsync();
                    var bookedSeatsData = JsonConvert.DeserializeObject<List<SeatNumbersDTO>>(bookedSeatsJson);

                    
                    var bookedSeats = bookedSeatsData.SelectMany(dto => dto.SeatNumbers).ToList();

                    return bookedSeats;
                }
                else
                {
                    // Show error message to the user
                    MessageBox.Show($"Failed to retrieve booked seats. Status code: {response.StatusCode}");
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
          
                MessageBox.Show($"Failed to retrieve booked seats: {ex.Message}");
                return new List<string>();
            }
        }

        public class SeatNumbersDTO
        {
            public List<string> SeatNumbers { get; set; }
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            
            var result = MessageBox.Show("Are you sure you want to confirm the booking?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
               if (selectedTrain != null) 
                {
                 
        
                    string selectedDate = selectedTrain.Date;
                    string nic = txbNIC.Text; 
                    List<string> selectedSeats = seatButtons.Where(seat => seat.IsSelected).Select(seat => seat.SeatId).ToList();

                    bool userAlreadyBooked;
                    try
                    {
                        userAlreadyBooked = await CheckIfUserAlreadyBooked(selectedTrain.TrainId.ToString(), selectedDate, nic, selectedSeats);

                        if (userAlreadyBooked)
                        {
                            MessageBox.Show("User already booked maximum count of seats.");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while checking existing bookings: {ex.Message}");
                    
                        return;
                    }

 
                    if (!userAlreadyBooked)
                    { 

                        await AddBooking(selectedTrain.TrainId.ToString(), selectedDate, nic, selectedSeats);
                        PassDataForPrintingTicket(nic, selectedDate, selectedTrain, selectedSeats);
                        await BookSelectedSeats();

                    }
                }
                else
                {
                    MessageBox.Show("Please select a train.");
                   
                }
            }
            else
            {
          
                MessageBox.Show("Booking cancelled.");
            }
        }

        private void PassDataForPrintingTicket(string nic, string date, MyTrain selectedTrain, List<string> selectedSeats)
        {
            if (selectedTrain != null && selectedSeats.Any())
            {
               
                string trainName = selectedTrain.Name;
                string startStation = selectedTrain.StartStation;
                string destinationStation = selectedTrain.DestinationStation;
                string ticketInfo = GenerateTicketInformation(trainName, startStation, destinationStation, date, nic, selectedSeats);

              
                PrintTicket(ticketInfo);
            }
            else
            {
                MessageBox.Show("Please select a train and seats before printing the ticket.");
            }
        }
        private string GenerateTicketInformation(string trainName, string startStation, string destinationStation, string date, string nic, List<string> selectedSeats)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Train Name: {trainName}");
            sb.AppendLine($"Start Station: {startStation}");
            sb.AppendLine($"Destination Station: {destinationStation}");
            sb.AppendLine($"Date: {date}");
            sb.AppendLine($"NIC: {nic}");
            sb.AppendLine("Selected Seats:");

            foreach (string seat in selectedSeats)
            {
                sb.AppendLine($"- {seat}");
            }

            return sb.ToString();
        }


        private void PrintTicket(string ticketInfo)
        {
            try
            {
              
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketInfo, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20); 

               
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Image Files (*.png)|*.png";
                saveFileDialog.FileName = "ticket_qr_code.png";

          
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                 
                    qrCodeImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);

                  
                    MessageBox.Show("QR code saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the QR code: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void dgvTrainData_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) // Check if the header row is clicked
                return;

            if (e.ColumnIndex == dgvTrainData.Columns["Column1"].Index)
            {
                // Get the selected train
                selectedTrain = (MyTrain)dgvTrainData.Rows[e.RowIndex].DataBoundItem;

                // Show a message to confirm the selected train
                if (selectedTrain != null)
                {
                    MessageBox.Show($"Selected Train: {selectedTrain.Name}");
                    // Other logic related to selectedTrain

                    // Perform booking here if needed
               
                    if (selectedTrain != null)
                    {
                        // Get capacities for First Class and Second Class
                        int firstClassCapacity = selectedTrain.Capacity > 20 ? 20 : selectedTrain.Capacity;
                        int secondClassCapacity = selectedTrain.Capacity > 50 ? 50 : selectedTrain.Capacity;

                        // Calculate Third Class capacity
                        int thirdClassCapacity = selectedTrain.Capacity - firstClassCapacity - secondClassCapacity;

                        // Perform booking here if needed
                        await LoadSeats(firstClassCapacity, secondClassCapacity, thirdClassCapacity);
                    }
                }
                else
                {
                    MessageBox.Show("No train selected.");
                }
            }
        }

        private async Task BookSelectedSeats()
        {
        

            Booking bookingRequest = CreateBookingRequest(); // New method call


            // Check if booking request is null
            if (bookingRequest == null)
            {
                Console.WriteLine("Booking request is null. Aborting...");
                return;
            }

            Console.WriteLine("Booking request: " + JsonConvert.SerializeObject(bookingRequest));

            // Call the API to book seats
            Console.WriteLine("Calling BookSeatsAsync method...");
            BookingResult bookingResult = await BookSeatsAsync(bookingRequest);
            Console.WriteLine("BookSeatsAsync method executed.");

            // Check if booking result is null
            if (bookingResult == null)
            {
                Console.WriteLine("Booking result is null. Aborting...");
                return;
            }

            Console.WriteLine("Booking result: " + JsonConvert.SerializeObject(bookingResult));

            // Show the result to the user
            MessageBox.Show(bookingResult.Message);

            // Clear selected train and seats
            selectedTrain = null;
            selectedSeats.Clear();

            Console.WriteLine("Selected train and seats cleared.");

            DisableSeatSelection();

            Console.WriteLine("BookSelectedSeats method completed.");

        
        }

        private async Task AddBooking(string trainId, string date, string nic, List<string> seats)
        {
            try
            {
                var booking = new
                {
                    TrainId = trainId,
                    Date = date,
                    Nic = nic,
                    Seats = seats
                };

        
                string jsonBooking = JsonConvert.SerializeObject(booking);

                HttpResponseMessage response = await _httpClient.PostAsync("https://localhost:7125/api/BookedUser/AddBooking", new StringContent(jsonBooking, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Booking data added successfully.");
                }
                else
                {
                    MessageBox.Show($"Failed to add booking data. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding booking data: {ex.Message}");
            }
        }


        private Booking CreateBookingRequest()
        {
            if (selectedTrain == null)
            {
                MessageBox.Show("Please select a train.");
                return null;
            }

            string selectedDate = selectedTrain.Date;

            List<string> selectedSeats = seatButtons.Where(seat => seat.IsSelected).Select(seat => seat.SeatId).ToList();

            if (selectedSeats.Count == 0)
            {
                MessageBox.Show("Please select seats to book.");
                return null;
            }

            return new Booking
            {
                TrainId = selectedTrain.TrainId.ToString(),
                Date = selectedDate,
                Seats = selectedSeats
            };
        }

        private async Task<BookingResult> BookSeatsAsync(Booking request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("https://localhost:7125/api/Booking/book", request);

                if (response.IsSuccessStatusCode)
                {
                    string bookingJson = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<BookingResult>(bookingJson);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to book seats. Status code: {response.StatusCode}\nError content: {errorContent}");
                    Console.WriteLine($"Failed to book seats. Status code: {response.StatusCode}\nError content: {errorContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to book seats: {ex.Message}");
                return null;
            }
        }

        private void SeatButton_Click(object sender, EventArgs e)
        {
            SeatButton selectedSeatButton = (SeatButton)sender;
            if (selectedSeatButton.IsSelected)
            {
                selectedSeatButton.BackColor = Color.Empty;
                selectedSeats.Remove(selectedSeatButton.SeatId);
                selectedSeatButton.IsSelected = false;
            }
            else
            {
                if (selectedSeats.Count < maxSeatsToSelect)
                {
                    selectedSeatButton.BackColor = Color.Green;
                    selectedSeats.Add(selectedSeatButton.SeatId);
                    selectedSeatButton.IsSelected = true;
                }
                else
                {
                    MessageBox.Show($"You can select up to {maxSeatsToSelect} seats.");
                }
            }
        }
        public class SeatButton : Button
        {
            public int Row { get; }
            public int Column { get; }
            public string SeatId => $"{ClassName}{(char)('A' + Row)}{Column + 1}";
            public bool IsSelected { get; set; }
            public string ClassName { get; } // Add ClassName property

            // Constructor with class name parameter
            public SeatButton(int row, int column, string className)
            {
                Row = row;
                Column = column;
                ClassName = className;
            }
        }

        private void DisableSeatSelection()
        {
            foreach (var seatButton in seatButtons)
            {
                seatButton.Enabled = false;
            }
        }


        private async Task<bool> CheckIfUserAlreadyBooked(string trainId, string date, string nic, List<string> seats)
        {
            try
            {
                // Construct the query string
                string queryString = $"?trainId={trainId}&date={date}&nic={nic}";

                // Create the HTTP request
                var httpRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://localhost:7125/api/BookedUser/CheckIfExists{queryString}")
                };

                // Send the HTTP request
                HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string resultJson = await response.Content.ReadAsStringAsync();

                    // Convert the response to a boolean value
                    bool canBook = JsonConvert.DeserializeObject<bool>(resultJson);

                    return canBook;
                }
                else
                {
                    MessageBox.Show($"Failed to check if user can book. Status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check if user can book: {ex.Message}");
                return false;
            }
        }


        public class BookingResponse
        {
            public string trainId { get; set; }
            public string date { get; set; }
            public string nic { get; set; }
            public List<string> seats { get; set; }
        }


        public class MyTrain
        {
            public string Name { get; set; }
            public string StartStation { get; set; }
            public string DestinationStation { get; set; }

            public int Capacity { get; set; }
            public string DepartureTime { get; set; }
            public string ArrivalTime { get; set; }

            public string Date { get; set; }

            public int TrainId { get; set; }
        }

        public class StationsDTO
        {
            public string[] StartStations { get; set; }
            public string[] DestinationStations { get; set; }
        }

        public class Booking
        {
            public string TrainId { get; set; }
            public string Date { get; set; }
            public List<string> Seats { get; set; }
        }
 


        public class BookingResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; }
        }

      
    }
}
