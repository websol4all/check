using Xunit;
using Xyzies.Devices.Services.Helpers;

namespace Xyzies.Devices.Tests.Unit_tests
{
    public class CalculationDistanceTests : IClassFixture<BaseTest>
    {
        [Fact]
        public void ShouldCalculeteLocationDeviceAsEnterIntoRadius()
        {
            // Arrange
            //Distanse between coordinats 682meters
            double latitudeOld = 48.423659;
            double longitudeOld = 35.121916;
            double latitudeNew = 48.419431;
            double longitudeNew = 35.128651;
            double radius = 700;

            // Act
            bool result = CalculateDistanceForDevice.DeviceIsInLocation(latitudeOld, longitudeOld, latitudeNew, longitudeNew, radius);

            //Assert
            Assert.Equal(true, result);
        }

        [Fact]
        public void ShouldCalculeteLocationDeviceAsNotEnterIntoRadius()
        {
            // Arrange
            //Distanse between coordinats 682meters
            double latitudeOld = 48.423659;
            double longitudeOld = 35.121916;
            double latitudeNew = 48.419431;
            double longitudeNew = 35.128651;
            double radius = 680;

            // Act
            bool result = CalculateDistanceForDevice.DeviceIsInLocation(latitudeOld, longitudeOld, latitudeNew, longitudeNew, radius);

            //Assert
            Assert.Equal(false, result);
        }
    }
}
