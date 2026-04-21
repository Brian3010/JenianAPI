using System.ComponentModel.DataAnnotations;

namespace Jenian.API.Contracts.Cwh
{

  public class StockUpdateDto
  {
    public int TrolleyOfStock { get; set; }
    public string? StockNote { get; set; }

    public int TrolleyOfCosmetics { get; set; }
    public string? CosmeticNote { get; set; }

    public int TrolleyofFragrances { get; set; }
    public string? FragranceNote { get; set; }

    public string? AdditionalStock { get; set; }
    public string? AdditionalNote { get; set; }

  }

  public class NightTasksDto
  {
    public string? DispLedge { get; set; }
    public string? Gondolas { get; set; }
    public string? Mesh { get; set; }
    public string? Tills { get; set; }
    public string? ClipStrips { get; set; }
    public string? Podiums { get; set; }
    public string? LowLevel { get; set; }
    public string? FloorStack { get; set; }
    public string? TopSellers { get; set; }
    public string? BatWings { get; set; }
    public string? Sunglasses { get; set; }
    public string? Catalogue { get; set; }

  }

  public class AislesFacingDto
  {

    public string? FrontCounter { get; set; }
    public string? FemHygSummer { get; set; }
    public string? Haircare { get; set; }
    public string? Skincare { get; set; }
    public string? Vitamins { get; set; }
    public string? PSA { get; set; }
    public string? Backwall { get; set; }
    public string? SportNutritions { get; set; }
    public string? BabyFirstAid { get; set; }
    public string? Cosmetics { get; set; }
    public string? Fragrances { get; set; }

  }

  public class CleaningDto
  {
    public string? BinRun { get; set; }
    public string? Sweeping { get; set; }
    public string? TeaRoom { get; set; }
    public string? ConsultingRoom { get; set; }

  }

  public class GeneralCheckDto
  {
    public required string FreeTrolleys { get; set; }
    public required string FreeCages { get; set; }
    public int NumOfClickCollect { get; set; }

    public int NumOfCataBundle { get; set; }
    public int NumOfMagaBundle { get; set; }
    public required string NumOfMyPals { get; set; }
    public required string NumOfFragKeys { get; set; }
    public required string NumOfLiftPasses { get; set; }
    public required string NumOfAugmodos { get; set; }
  }


  public class CWHReportRequestDTOs
  {
    [Required]
    [MaxLength(5, ErrorMessage = "You can upload a maximum of 5 photos.")]
    public required List<IFormFile> DeliveryScreenShots { get; set; } = new();

    [Required]
    public required StockUpdateDto StockUpdate { get; set; } = new();

    [Required]
    public required NightTasksDto NightTasks { get; set; } = new();

    [Required]
    public required AislesFacingDto AislesFacing { get; set; } = new();

    [Required]
    public required CleaningDto Cleaning { get; set; } = new();

    [Required]
    public required GeneralCheckDto GeneralCheck { get; set; }

    public string? AdditionalTasks { get; set; }


  }
}
