using Moq;
using NUnit.Framework;
using ProzorroDataMining.Application.ApplicationContracts;
using ProzorroDataMining.Application.RepositoryContracts;
using ProzorroDataMining.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace ProzorroDataMining.Api.Tests
{
    public class AnalyticsControllerTests
    {
        [Test]
        public async System.Threading.Tasks.Task GetTenderSavings_ReturnsNotFound_WhenMissing()
        {
            var svc = new Mock<IAnalyticsService>();
            svc.Setup(r => r.GetBudgetSavingsByTenderIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((decimal?)null);

            var controller = new AnalyticsController(svc.Object);

            var result = await controller.GetTenderSavings("missing", CancellationToken.None);

            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async System.Threading.Tasks.Task GetSummary_ReturnsOk()
        {
            var svc = new Mock<IAnalyticsService>();
            svc.Setup(r => r.GetTopProcuringEntitiesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { new NameValueDto("A", 1m) });
            svc.Setup(r => r.GetTopSuppliersAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { new NameValueDto("B", 2m) });

            var controller = new AnalyticsController(svc.Object);

            var result = await controller.GetTopProcuringEntities(5, CancellationToken.None) as OkObjectResult;

            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}
