using AutoMapper;
using Reviews.Application.Reviews;
using Reviews.Domain.Entities;

namespace Reviews.Application.Mapping;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        // ObjectId -> string, вкладений Value Object та масив коментарів -> прості значення DTO
        CreateMap<Review, ReviewDto>()
            .ForCtorParam("Id", o => o.MapFrom(s => s.Id.ToString()))
            .ForCtorParam("AuthorName", o => o.MapFrom(s => s.Author.DisplayName))
            .ForCtorParam("CommentsCount", o => o.MapFrom(s => s.Comments.Count));
    }
}
