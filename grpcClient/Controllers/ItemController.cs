using Grpc.Core;
using grpcClient.Dtos;
using grpcClient.Services;
using Microsoft.AspNetCore.Mvc;

namespace grpcClient.Controllers
{
    [Route("api/items")]
    [ApiController]
    public class ItemController (ItemGrpcService.ItemGrpcServiceClient grpcClient) : ControllerBase
    {
        [HttpPost("search")]
        public async Task<IActionResult> GetItems(SearchItemsDto dto)
        {
            try
            {
                var request = new GetItemsRequest();
                if (dto.ItemIds != null) 
                    request.Ids.AddRange(dto.ItemIds.Select(id => id.ToString()));

                var response = await grpcClient.GetItemsAsync(request);
                return Ok(response.Items);
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
            {
                return BadRequest(ex.Status.Detail);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, $"gRPC error: {ex.Status.Detail}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<AddItemResponse>> AddItem([FromBody] AddItemDto dto)
        {
            try
            {
                var request = new AddItemRequest()
                {
                    Name = dto.Name,
                    Quantity = dto.Quantity,
                    Price = dto.Price
                };
                var response = await grpcClient.AddItemAsync(request);
                return CreatedAtAction("AddItem", new { id = response.Id }, response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, $"Failed to add item: {ex.Status.Detail}");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateItemDto dto)
        {
            try
            {
                var request = new UpdateItemRequest()
                {
                    Id = id.ToString(),
                    Name = dto.Name,
                    Quantity = dto.Quantity,
                    Price = dto.Price
                };
                var response = await grpcClient.UpdateItemAsync(request);
                return Ok(response.UpdatedItem);
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return NotFound();
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
            {
                return BadRequest(ex.Status.Detail);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            try
            {
                var response = await grpcClient.DeleteItemAsync(new DeleteItemRequest { Id = id.ToString() });
                return response.Success
                    ? Ok()
                    : BadRequest();
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                return NotFound();
            }
        }
    }
}
