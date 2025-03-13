using AutoMapper;
using NorthwindStore.BL;

namespace NorthwindStore.Tests.UnitTests
{
    public class AutoMapperTests
    {
        [Fact]
        public void TestMappings()
        {
            var config = new MapperConfiguration(config =>
            {
                config.AddMaps(typeof(BusinessLayer).Assembly);
            });
            config.AssertConfigurationIsValid();
        }
    }
}