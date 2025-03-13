using Castle.Facilities.TypedFactory;
using Castle.Windsor;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NorthwindStore.App.Installers;
using NorthwindStore.BL.DTO;
using NorthwindStore.BL.Queries;
using Riganti.Utils.Infrastructure.Core;

namespace NorthwindStore.Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class QueryTests
    {
        private readonly IServiceProvider serviceProvider;

        private readonly IUnitOfWorkProvider unitOfWorkProvider;

        public QueryTests(EnsureDatabaseReadyFixture fixture)
        {
            var container = new WindsorContainer();
            container.AddFacility<TypedFactoryFacility>();
            container.Install(
                new DataAccessInstaller(fixture.Configuration.GetConnectionString("DB")),
                new FacadeInstaller(AppContext.BaseDirectory),
                new PresentationInstaller(),
                new AutoMapperInstaller()
            );

            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            serviceProvider = WindsorRegistrationHelper.CreateServiceProvider(container, services);

            unitOfWorkProvider = serviceProvider.GetRequiredService<IUnitOfWorkProvider>();
        }

        [Fact]
        public void TestProductListQuery()
        {
            using var uow = unitOfWorkProvider.Create();

            var query = serviceProvider.GetRequiredService<ProductListQuery>();
            query.Filter = new ProductFilterDTO();
            var results = query.Execute();

            Assert.True(results.Count > 0);
        }
    }
    
}