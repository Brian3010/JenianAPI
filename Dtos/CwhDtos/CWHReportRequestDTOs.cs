using System.ComponentModel.DataAnnotations;

namespace JenianAPI.Dtos.CwhDtos
{

  public class StockUpdate
  {
    public int TrolleyOfStock { get; set; }
    public string? StockNote { get; set; }

    public int TrolleyOfCosmetics { get; set; }
    public string? CosmeticNote { get; set; }

    public int TrolleyofFragrances { get; set; }
    public string? FragranceNote { get; set; }

    public int? AdditionalStock { get; set; }
    public string? AdditionalNote { get; set; }

  }

  public class NightTasks
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

  public class AislesFacing
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

  public class Cleaning
  {
    public string? BinRun { get; set; }
    public string? Sweeping { get; set; }
    public string? TeaRoom { get; set; }
    public string? ConsultingRoom { get; set; }

  }

  public class GeneralCheck
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
    public required List<IFormFile> DeliveryScreenShots { get; set; } = new();

    [Required]
    public required StockUpdate StockUpdate { get; set; } = new();

    [Required]
    public required NightTasks NightTasks { get; set; } = new();

    [Required]
    public required AislesFacing AislesFacing { get; set; } = new();

    [Required]
    public required Cleaning Cleaning { get; set; } = new();

    //[Required]
    //public required GeneralCheck GeneralCheck { get; set; }

    public string? AdditionalTasks { get; set; }


  }
}
