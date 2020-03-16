using Mapster;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Services.Models.Comment;
using Xyzies.Devices.Services.Models.DeviceModels;

namespace Xyzies.Devices.Services.Mappers
{
    public class MapperConfigure
    {
        public static void Configure()
        {
            TypeAdapterConfig<Device, DeviceModel>.NewConfig()
               .IgnoreIf((src, dest) => dest.Branch != null, dest => dest.Branch.Id)
               .IgnoreIf((src, dest) => dest.Company != null, dest => dest.Company.Id)
               .Map(dest => dest.Branch.Id, src => src.BranchId)
               .Map(dest => dest.Company.Id, src => src.CompanyId);

            TypeAdapterConfig<Comment, CommentModel>.NewConfig()
                .Map(dest => dest.Comment, src => src.Message);
        }
    }
}
