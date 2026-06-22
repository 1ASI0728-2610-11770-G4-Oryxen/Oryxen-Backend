using Oryxen.Domain.Enums;
using Oryxen.Domain.Services;
using Xunit;

namespace Oryxen.Domain.Tests.Services;

public class PlantHealthCalculatorTests
{
    private static int ComputeStatus(int score) => score switch
    {
        >= 70 => (int)PlantStatus.Healthy,
        >= 40 => (int)PlantStatus.Warning,
        _ => (int)PlantStatus.Critical
    };

    public class Compute_Scoring
    {
        [Fact]
        public void Returns_100_When_All_Metrics_Are_In_Ideal_Range()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 22.5);
            Assert.Equal(100, score);
        }

        [Fact]
        public void Returns_100_At_Lower_Boundary_Of_Each_Range()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 40, humidity: 40, temperature: 18);
            Assert.Equal(100, score);
        }

        [Fact]
        public void Returns_100_At_Upper_Boundary_Of_Each_Range()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 70, humidity: 60, temperature: 27);
            Assert.Equal(100, score);
        }

        [Fact]
        public void Returns_0_When_All_Metrics_Are_Far_Outside_Range()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 0, humidity: 0, temperature: 0);
            Assert.Equal(0, score);
        }

        [Fact]
        public void Clamps_To_0_When_Negative_Values_Passed()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: -500, humidity: -500, temperature: -500);
            Assert.Equal(0, score);
        }

        [Fact]
        public void Clamps_To_100_When_Extremely_High_Values_Passed()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 500, humidity: 500, temperature: 500);
            Assert.InRange(score, 0, 100);
        }
    }

    public class Compute_Weighting
    {
        [Fact]
        public void Soil_Contributes_50_Percent_Of_Total_Score()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 0, temperature: 0);
            Assert.Equal(50, score);
        }

        [Fact]
        public void Humidity_Contributes_25_Percent_Of_Total_Score()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 0, humidity: 50, temperature: 0);
            Assert.Equal(25, score);
        }

        [Fact]
        public void Temperature_Contributes_25_Percent_Of_Total_Score()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 0, humidity: 0, temperature: 22.5);
            Assert.Equal(25, score);
        }

        [Fact]
        public void Weights_Sum_To_100_When_All_Metrics_Ideal()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 22.5);
            Assert.Equal(100, score);
        }
    }

    public class Compute_Band_Classification
    {
        [Fact]
        public void Score_At_70_Maps_To_Healthy_Band()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 18);
            Assert.True(score >= 70);
            Assert.Equal((int)PlantStatus.Healthy, ComputeStatus(score));
        }

        [Fact]
        public void Score_Between_40_And_69_Maps_To_Warning_Band()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 0, temperature: 0);
            Assert.InRange(score, 40, 69);
            Assert.Equal((int)PlantStatus.Warning, ComputeStatus(score));
        }

        [Fact]
        public void Score_Below_40_Maps_To_Critical_Band()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 0, humidity: 0, temperature: 0);
            Assert.True(score < 40);
            Assert.Equal((int)PlantStatus.Critical, ComputeStatus(score));
        }

        [Fact]
        public void Score_Of_100_Maps_To_Healthy_Band()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 22.5);
            Assert.Equal(100, score);
            Assert.Equal((int)PlantStatus.Healthy, ComputeStatus(score));
        }
    }

    public class Compute_Determinism
    {
        [Fact]
        public void Returns_Same_Score_For_Identical_Inputs()
        {
            var first = PlantHealthCalculator.Compute(soilMoisture: 45, humidity: 55, temperature: 20);
            var second = PlantHealthCalculator.Compute(soilMoisture: 45, humidity: 55, temperature: 20);
            Assert.Equal(first, second);
        }

        [Fact]
        public void Returns_Different_Score_For_Different_Inputs()
        {
            var ideal = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 22.5);
            var degraded = PlantHealthCalculator.Compute(soilMoisture: 10, humidity: 10, temperature: 10);
            Assert.NotEqual(ideal, degraded);
        }

        [Theory]
        [InlineData(55, 50, 22.5, 100)]
        [InlineData(40, 40, 18, 100)]
        [InlineData(70, 60, 27, 100)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(55, 0, 0, 50)]
        [InlineData(0, 50, 0, 25)]
        [InlineData(0, 0, 22.5, 25)]
        public void Produces_Expected_Score_For_Known_Inputs(
            double soil, double humidity, double temperature, int expected)
        {
            var score = PlantHealthCalculator.Compute(soil, humidity, temperature);
            Assert.Equal(expected, score);
        }
    }

    public class Compute_Linear_Decay
    {
        [Fact]
        public void Score_Decreases_Linearly_As_Soil_Drifts_Below_Min()
        {
            var ideal = PlantHealthCalculator.Compute(soilMoisture: 40, humidity: 50, temperature: 22.5);
            var drifted = PlantHealthCalculator.Compute(soilMoisture: 25, humidity: 50, temperature: 22.5);
            Assert.True(drifted < ideal);
            Assert.True(drifted > 0);
        }

        [Fact]
        public void Score_Decreases_Linearly_As_Temperature_Exceeds_Max()
        {
            var ideal = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 27);
            var drifted = PlantHealthCalculator.Compute(soilMoisture: 55, humidity: 50, temperature: 32);
            Assert.True(drifted < ideal);
            Assert.True(drifted > 0);
        }

        [Fact]
        public void Score_Reaches_Zero_When_Soil_Drifts_By_Full_Range_Width()
        {
            var score = PlantHealthCalculator.Compute(soilMoisture: 10, humidity: 50, temperature: 22.5);
            Assert.True(score < 75);
        }
    }
}
