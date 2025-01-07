using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain;

public class Image : BaseDomainModel
{
    [Column(TypeName = "nvarchar(4000)")]
    public string? Url { get; set; }
    public int ProductId { get; set; }
    public string? PublicCode{get;set;}
}