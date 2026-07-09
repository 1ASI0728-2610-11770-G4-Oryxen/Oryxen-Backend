using NSubstitute;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Plants;
using Oryxen.Application.Plants.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;
using Xunit;

namespace Oryxen.Application.Tests.Plants;

/// <summary>
/// Object-level authorization (ownership) tests for the Plant Management service:
/// a FARMER may only operate on their own plants (403 otherwise), an ADMIN on any plant.
/// These back the Gherkin RBAC spec (01-autenticacion-rbac.feature).
/// </summary>
public class PlantServiceOwnershipTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid IntruderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AdminId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly IPlantRepository _plants = Substitute.For<IPlantRepository>();
    private readonly ITelemetryRepository _telemetry = Substitute.For<ITelemetryRepository>();
    private readonly IWeatherService _weather = Substitute.For<IWeatherService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly PlantService _sut;
    private readonly Plant _plant;

    public PlantServiceOwnershipTests()
    {
        _sut = new PlantService(_plants, _telemetry, _weather, _unitOfWork);

        _plant = new Plant
        {
            UserAccountId = OwnerId,
            Name = "Aloe Vera",
            Type = "Succulent",
            Location = "Lima"
        };

        _plants.GetByIdAsync(_plant.Id, Arg.Any<CancellationToken>()).Returns(_plant);
        _telemetry.GetByPlantAsync(_plant.Id, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TelemetryData>());
    }

    [Fact]
    public async Task Owner_Can_Read_Their_Plant()
    {
        var response = await _sut.GetByIdAsync(_plant.Id, OwnerId, isAdmin: false);

        Assert.Equal(_plant.Id, response.Id);
        Assert.Equal(OwnerId, response.UserId);
    }

    [Fact]
    public async Task NonOwner_Farmer_Gets_403_On_Read()
    {
        await Assert.ThrowsAsync<PlantAccessDeniedException>(() =>
            _sut.GetByIdAsync(_plant.Id, IntruderId, isAdmin: false));
    }

    [Fact]
    public async Task Admin_Can_Read_Any_Plant()
    {
        var response = await _sut.GetByIdAsync(_plant.Id, AdminId, isAdmin: true);

        Assert.Equal(_plant.Id, response.Id);
    }

    [Fact]
    public async Task NonOwner_Farmer_Gets_403_On_Update()
    {
        var request = new UpdatePlantRequest { Name = "Hacked", Type = "Cactus" };

        await Assert.ThrowsAsync<PlantAccessDeniedException>(() =>
            _sut.UpdateAsync(_plant.Id, IntruderId, isAdmin: false, request));

        Assert.Equal("Aloe Vera", _plant.Name); // unchanged
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Owner_Can_Update_Their_Plant()
    {
        var request = new UpdatePlantRequest { Name = "Aloe Prime", Type = "Succulent" };

        var response = await _sut.UpdateAsync(_plant.Id, OwnerId, isAdmin: false, request);

        Assert.Equal("Aloe Prime", response.Name);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonOwner_Farmer_Gets_403_On_Delete()
    {
        await Assert.ThrowsAsync<PlantAccessDeniedException>(() =>
            _sut.DeleteAsync(_plant.Id, IntruderId, isAdmin: false));

        _plants.DidNotReceive().Remove(Arg.Any<Plant>());
    }

    [Fact]
    public async Task Admin_Can_Delete_Any_Plant()
    {
        await _sut.DeleteAsync(_plant.Id, AdminId, isAdmin: true);

        _plants.Received(1).Remove(_plant);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonOwner_Farmer_Gets_403_On_Watering()
    {
        await Assert.ThrowsAsync<PlantAccessDeniedException>(() =>
            _sut.WaterAsync(_plant.Id, IntruderId, isAdmin: false, new WaterPlantRequest()));

        Assert.Empty(_plant.WateringLogs);
    }

    [Fact]
    public async Task NonOwner_Farmer_Gets_403_On_Sensor_Assignment()
    {
        var request = new AssignSensorRequest { DeviceId = "SL-999999" };

        await Assert.ThrowsAsync<PlantAccessDeniedException>(() =>
            _sut.AssignSensorAsync(_plant.Id, IntruderId, isAdmin: false, request));
    }

    [Fact]
    public async Task Missing_Plant_Still_Returns_404_Not_403()
    {
        var unknownId = Guid.NewGuid();
        _plants.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Plant?)null);

        await Assert.ThrowsAsync<PlantNotFoundException>(() =>
            _sut.GetByIdAsync(unknownId, OwnerId, isAdmin: false));
    }
}
