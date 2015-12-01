using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UdpSensorRelayer;

namespace SensorRelayerUTest
{
    [TestClass]
    public class RelayUnitTest
    {
        private RelayingClient _client;
        private string _testString;
        [TestInitialize]
        public void CreateClient()
        {
            _client = new RelayingClient(7000);
            _testString =
                "RoomSensor Broadcasting\n" +
                "Location:Teachers room\n" +
                "Platform: Linux - 3.12.28 + -armv6l - with - debian - 7.6\n" +
                "Machine:armv6l\n" +
                "Potentiometer(8bit): 135\n" +
                "Light Sensor(8bit): 195\n" +
                "Temperature(8bit): 218\n" +
                "Movement last detected: 2015 - 11 - 26 08:05:56.060697";
        }

        [TestMethod]
        public void SaveToDatabaseTest()
        {
            Notificaton testNotification = new Notificaton()
            {
                Location = "Unit-Test",
                MachineName = "Unit testing machine",
                MovementDetected = "#UNIT-TEST",
            };
            _client.SaveToDatabase(testNotification);
            var allNotifications = _client.GetNotifications();
            var newest = allNotifications[allNotifications.Count - 1];
            Assert.IsTrue(newest.MovementDetected == testNotification.MovementDetected);
        }

        [TestMethod]
        public void GetNotificationsTest()
        {
            var allNotifications = _client.GetNotifications();
            Assert.IsNotNull(allNotifications);
        }

        [TestMethod]
        public void FilterMeasurementsTest()
        {
            var notificationModel = _client.FilterMeasurements(_testString);
            Assert.IsTrue(notificationModel.MachineName == "Machine:armv6l"
                && notificationModel.Location == "Location:Teachers room" 
                && notificationModel.MovementDetected == "2015 - 11 - 26 08:05:56.060697");
        }
    }
}
