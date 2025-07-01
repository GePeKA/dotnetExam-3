using Grpc.Core;
using grpcServer.Entities;
using grpcServer.Repositories;

namespace grpcServer.Services
{
    public class ItemService(IItemRepository itemRepository): ItemGrpcService.ItemGrpcServiceBase
    {
        public override async Task<GetItemsResponse> GetItems(GetItemsRequest request, ServerCallContext context)
        {
            var invalidIds = request.Ids
                .Where(id => !Guid.TryParse(id, out _))
                .ToList();

            if (invalidIds.Count != 0)
            {
                context.Status = new Status(
                    StatusCode.InvalidArgument,
                    $"Invalid GUID format: {string.Join(", ", invalidIds)}"
                );
                return new GetItemsResponse();
            }

            var guids = request.Ids.Select(Guid.Parse).ToList();
            var items = await itemRepository.GetItemsAsync(guids);

            var response = new GetItemsResponse();
            response.Items.AddRange(items.Select(item => new ItemDto
            {
                Id = item.Id.ToString(),
                Name = item.Name,
                Quantity = item.Quantity,
                Price = item.Price
            }));

            return response;
        }

        public override async Task<AddItemResponse> AddItem(AddItemRequest request, ServerCallContext context)
        {
            var newItem = new Item()
            {
                Id = new Guid(),
                Name = request.Name,
                Price = request.Price,
                Quantity = request.Quantity
            };
            var result = await itemRepository.AddItemAsync(newItem);

            return new AddItemResponse() { Id = result.ToString() };
        }

        public override async Task<DeleteItemResponse> DeleteItem(DeleteItemRequest request, ServerCallContext context)
        {
            if (Guid.TryParse(request.Id, out var guid))
            {
                var deleteResult = await itemRepository.DeleteItemAsync(guid);

                return new DeleteItemResponse() { Success = deleteResult };
            }
            else
            {
                context.Status = new Status(
                    StatusCode.InvalidArgument,
                    $"Invalid GUID format: {request.Id}"
                );
                return new DeleteItemResponse() { Success = false };
            }
        }

        public override async Task<UpdateItemResponse> UpdateItem(UpdateItemRequest request, ServerCallContext context)
        {
            if (Guid.TryParse(request.Id, out var guid))
            {
                var updatedItem = new Item()
                {
                    Id = Guid.Parse(request.Id),
                    Name = request.Name,
                    Price = request.Price,
                    Quantity = request.Quantity
                };
                var updateResult = await itemRepository.UpdateItemAsync(updatedItem);
                
                if (updatedItem == null)
                {
                    context.Status = new Status(
                        StatusCode.NotFound,
                        $"Item with ID '{request.Id}' not found"
                    );
                    return new UpdateItemResponse();
                }

                var itemDto = new ItemDto()
                {
                    Id = updateResult!.Id.ToString(),
                    Name = updateResult.Name,
                    Price = updateResult.Price,
                    Quantity = updateResult.Quantity
                };
                return new UpdateItemResponse() { UpdatedItem = itemDto };
            }
            else
            {
                context.Status = new Status(
                    StatusCode.InvalidArgument,
                    $"Invalid GUID format: {request.Id}"
                );
                return new UpdateItemResponse();
            }
        }
    }
}
