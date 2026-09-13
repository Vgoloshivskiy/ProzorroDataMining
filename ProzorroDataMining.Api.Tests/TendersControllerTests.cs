using Moq;
using NUnit.Framework;
using ProzorroDataMining.Application.RepositoryContracts;
using ProzorroDataMining.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Collections.Generic;

namespace ProzorroDataMining.Api.Tests
{
    public class TendersControllerTests
    {
        [Test]
        public async System.Threading.Tasks.Task GetTenders_ReturnsOk()
        {
            var repo = new Mock<ITenderRepository>();
            repo.Setup(r => r.GetTenderListAsync(1, 100, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TenderListItemDto> { new TenderListItemDto("id1", "Name1") });

            var controller = new TendersController(repo.Object);

            var result = await controller.GetTenders(1, 100, CancellationToken.None) as OkObjectResult;

            Assert.IsNotNull(result);
        }
    }
}
