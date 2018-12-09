using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.Commands.Models;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.WebApi.Filters;
using Sds.Osdr.WebApi.Requests;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [UserInfoRequired]
    public class NodeCollectionsController : Controller
    {
        private IBusControl _bus;
        private Guid UserId => Guid.Parse(User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value);

        public NodeCollectionsController(IBusControl bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>
        /// Manipulation with node collection - move to another node or delete
        /// </summary>
        /// <param name="request">Json patch document describing folder manipulation according http://jsonpatch.com </param>
        /// <returns></returns>
        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody]JsonPatchDocument<UpdateNodeCollectionRequest> request)
        {
            var nodeCollection = new UpdateNodeCollectionRequest();

            request.ApplyTo(nodeCollection);

            foreach (var deletedItem in nodeCollection.Deleted)
            {
                switch (deletedItem.Type)
                {
                    case "Model":
                        await _bus.Publish<DeleteModel>(new
                        {
                            Id = deletedItem.Id,
                            UserId = UserId,
                            ExpectedVersion = deletedItem.Version,
                            Force = false
                        });

                    break;

                    case "File":
                        await _bus.Publish<DeleteFile>(new
                        {
                            Id = deletedItem.Id,
                            UserId = UserId,
                            ExpectedVersion = deletedItem.Version,
                            Force = false
                        });

                        break;

                    case "Folder":
                        await _bus.Publish<DeleteFolder>(new
                        {
                            Id = deletedItem.Id,
                            UserId = UserId,
                            CorrelationId = deletedItem.CorrelationId,
                            ExpectedVersion = deletedItem.Version,
                            Force = false
                        });
                        break;

                    default:
                        break;
                }
            }

            foreach (var forceDeletedItem in nodeCollection.ForceDeleted)
            {
                switch (forceDeletedItem.Type)
                {
                    case "Model":
                        await _bus.Publish<DeleteModel>(new
                        {
                            Id = forceDeletedItem.Id,
                            UserId = UserId,
                            ExpectedVersion = forceDeletedItem.Version,
                            Force = true
                        });

                        break;

                    case "File":
                        await _bus.Publish<DeleteFile>(new
                        {
                            Id = forceDeletedItem.Id,
                            UserId = UserId,
                            ExpectedVersion = forceDeletedItem.Version,
                            Force = true
                        });

                        break;

                    case "Folder":
                        await _bus.Publish<DeleteFolder>(new
                        {
                            Id = forceDeletedItem.Id,
                            UserId = UserId,
                            CorrelationId = forceDeletedItem.CorrelationId,
                            ExpectedVersion = forceDeletedItem.Version,
                            Force = true
                        });
                        break;

                    default:
                        break;
                }
            }

            foreach (var movedItem in nodeCollection.Moved)
            {
                switch (movedItem.Type)
                {
                    case "Model":
                        await _bus.Publish<MoveModel>(new
                        {
                            Id = movedItem.Id,
                            UserId = UserId,
                            NewParentId = nodeCollection.ParentId,
                            ExpectedVersion = movedItem.Version
                        });
                        break;

                    case "File":
                        await _bus.Publish<MoveFile>(new
                        {
                            Id = movedItem.Id,
                            UserId = UserId,
                            NewParentId = nodeCollection.ParentId,
                            ExpectedVersion = movedItem.Version
                        });
                        break;

                    case "Folder":
                        await _bus.Publish<MoveFolder>(new
                        {
                            Id = movedItem.Id,
                            UserId = UserId,
                            NewParentId = nodeCollection.ParentId,
                            ExpectedVersion = movedItem.Version
                        });
                        break;

                    default:
                        break;
                }
            }

            return Accepted();
        }
    }
}
