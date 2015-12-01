using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UdpSensorRelayer.EmailWebService;

namespace UdpSensorRelayer
{
    /// <summary>
    ///     This class is used to listen for broadcasts, and has methods to filter out and save
    ///     strings to the database.
    /// </summary>
    public class RelayingClient
    {
        private readonly int _portNr;

        /// <summary>
        ///     The constructor takes in the port number to be used.
        /// </summary>
        /// <param name="portNr"></param>
        public RelayingClient(int portNr)
        {
            _portNr = portNr;
        }

        /// <summary>
        ///     Checks the last saved notification and the new one provided, and only saves it
        ///     to the database if they are not the same.
        /// </summary>
        /// <param name="motionNotification">The notification model to save to the database.</param>
        public void SaveToDatabase(Notificaton motionNotification)
        {
            var allNotifications = GetNotifications();
            var lastMeasurement = allNotifications[allNotifications.Count - 1];

            //Todo: Sometimes the sensor sends [No movement detected], make it filter it out if needed.
            if (lastMeasurement.MovementDetected != motionNotification.MovementDetected)
            {
                SaveNotification(motionNotification);
                SendToUsers(motionNotification);
            }
            Thread.Sleep(5000);
        }

        /// <summary>
        ///     Listens for a broadcast on the port, and then attempts to save
        ///     it to the database.
        /// </summary>
        public void ListenForBroadcast()
        {
            var done = false;

            var listener = new UdpClient(_portNr);
            var groupEP = new IPEndPoint(IPAddress.Any, _portNr);

            try
            {
                while (!done)
                {
                    Console.WriteLine("Waiting for broadcast");
                    var bytes = listener.Receive(ref groupEP);
                    var allMeasurements = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    var motionNotification = FilterMeasurements(allMeasurements);
                    SaveToDatabase(motionNotification);
                    Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                        groupEP,
                        Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Close();
            }
        }

        /// <summary>
        ///     Takes the whole string from the Raspberry Pi, and returns a notification model
        ///     with all required fields filled.
        /// </summary>
        /// <param name="measurements">The whole string that the Pi sent.</param>
        /// <returns></returns>
        public Notificaton FilterMeasurements(string measurements)
        {
            var allMeasurements = measurements.Split('\n');

            //Here we remove the "Motion Last Det..." part take and use only the datetime part
            //for some reason it was not saved if we used the entire string
            var movementDetectedString = allMeasurements[7].Remove(0, 24);

            var notificationToReturn = new Notificaton
            {
                Location = allMeasurements[1],
                MachineName = allMeasurements[3],
                MovementDetected = movementDetectedString
            };
            return notificationToReturn;
        }

        /// <summary>
        ///     Returns a list of all the notifications from the database.
        /// </summary>
        /// <returns>A list of the notifications.</returns>
        public List<Notificaton> GetNotifications()
        {
            var httpClient = new HttpClient {BaseAddress = new Uri(@"http://3rdsemesterwebapp.azurewebsites.net/api/")};
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var notificationsAsJson = Task.Run(async () => await httpClient.GetAsync("notificatons"));
            var allNotifications =
                notificationsAsJson.Result.Content.ReadAsAsync<IEnumerable<Notificaton>>().Result.ToList();
            return allNotifications;
        }

        /// <summary>
        ///     Attempts to save the notification provided to the database.
        /// </summary>
        /// <param name="notificationToSave">The notification to save.</param>
        private void SaveNotification(Notificaton notificationToSave)
        {
            var httpClient = new HttpClient {BaseAddress = new Uri(@"http://3rdsemesterwebapp.azurewebsites.net/api/")};
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.PostAsJsonAsync("notificatons", notificationToSave);
        }

        /// <summary>
        ///     Sends an notification email to all the registered users.
        /// </summary>
        /// <param name="notificationModel">The notifcation to send.</param>
        public void SendToUsers(Notificaton notificationModel)
        {
            var httpClient = new HttpClient {BaseAddress = new Uri(@"http://3rdsemesterwebapp.azurewebsites.net/api/") };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var profilesAsJson = Task.Run(async () => await httpClient.GetAsync("profiles"));
                var allProfiles =
                    profilesAsJson.Result.Content.ReadAsAsync<IEnumerable<Profile>>().Result.ToList();

                if (allProfiles.Count != 0)
                {
                    //Todo:This line is commented for testing purposes, un-comment to send the emails.
                    //allProfiles.ForEach(profile => SendEmail(profile.EmailAddress, notificationModel));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     Sends a single notification email to the provided email address.
        /// </summary>
        /// <param name="emailAddress">The email to send to.</param>
        /// <param name="notificationModel">The notification to send.</param>
        private void SendEmail(string emailAddress, Notificaton notificationModel)
        {
            var emailClient = new EmailServiceClient();
            emailClient.CreateEmailAndSend(emailAddress,
                "Movement was detected from machine:" + notificationModel.MachineName,
                "The message received:" + notificationModel.MovementDetected);
        }
    }
}