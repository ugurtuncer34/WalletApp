using AutoMapper;

namespace WalletBackend.Mapping;

public class TransactionProfile : Profile
{
    public TransactionProfile()
    {
        // Entity -> Read DTO
        CreateMap<Transaction, TransactionReadDto>()
            .ForMember(d => d.Direction, opt => opt.MapFrom(s => s.Direction.ToString()))
            .ForMember(d => d.AccountName, opt => opt.MapFrom(s => s.Account.Name))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name));

        // Create DTO -> Entity
        CreateMap<TransactionCreateDto, Transaction>();

        // Update DTO -> Entity (for PUT)
        CreateMap<TransactionUpdateDto, Transaction>()
            .ForAllMembers(opts => opts.Condition((src, _, srcMember) => srcMember != null));
        // ensures null values don't overwrite existing ones; optional
    }
}