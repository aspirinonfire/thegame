using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TheGame.Infra.AppConfiguration.Sections;

namespace TheGame.Infra.AppConfiguration;

public sealed record TheGameInfraConfig
{
  [Required]
  public PulumiConfiguration PulumiConfig { get; set; } = default!;

  [Required]
  public ExistingResourcesConfiguration ExistingResources { get; set; } = default!;

  [Required]
  public AzureSqlConfiguration AzureSqlServer { get; set; } = default!;

  [Required]
  public ContainerAppInfraConfiguration ContainerApp { get; set; } = default!;

  [Required]
  public GameApiConfiguration GameApi { get; set; } = default!;

  [Required]
  public StaticWebAppConfig StaticWebApp { get; set;} = default!;

  public IReadOnlyCollection<string> GetValidationErrors()
  {
    var validationResults = new List<ValidationResult>();

    var isTopLevelValid = Validator.TryValidateObject(this, new ValidationContext(this), validationResults, true);

    var pulumiConfig = PulumiConfig ?? new PulumiConfiguration();
    var isPulumiValid = Validator.TryValidateObject(pulumiConfig, new ValidationContext(pulumiConfig), validationResults, true);

    var existingResourcesConfig = ExistingResources ?? new ExistingResourcesConfiguration();
    var isExistingValid = Validator.TryValidateObject(existingResourcesConfig, new ValidationContext(existingResourcesConfig), validationResults, true);

    var sql = AzureSqlServer ?? new AzureSqlConfiguration();
    var isSqlValid = Validator.TryValidateObject(sql, new ValidationContext(sql), validationResults, true);

    var containerAppConfig = ContainerApp ?? new ContainerAppInfraConfiguration();
    var isContainerAppValid = Validator.TryValidateObject(containerAppConfig, new ValidationContext(containerAppConfig), validationResults, true);

    var apiConfig = GameApi ?? new GameApiConfiguration();
    var isApiConfigValid = Validator.TryValidateObject(apiConfig, new ValidationContext(apiConfig), validationResults, true);

    var swa = StaticWebApp ?? new StaticWebAppConfig();
    var isSwaValid = Validator.TryValidateObject(swa, new ValidationContext(swa), validationResults, true);

    if (isTopLevelValid &&
        isPulumiValid &&
        isExistingValid &&
        isSqlValid &&
        isContainerAppValid &&
        isApiConfigValid &&
        isSwaValid)
    {
      return [];
    }

    return validationResults
      .Select(error => $"{string.Join(", ", error.MemberNames)}: {error.ErrorMessage ?? "n/a"}")
      .ToList()
      .AsReadOnly();
  }

  /// <summary>
  /// Get Default tags
  /// </summary>
  /// <remarks>
  /// <list type="number">
  ///     <item>Project</item>
  ///     <item>Environment</item>
  ///     <item>managedBy</item>
  /// </list>
  /// </remarks>
  /// <returns></returns>
  public Pulumi.InputMap<string> GetDefaultTags() => new()
  {
        { "Project", PulumiConfig.ProjectName },
        { "Environment", PulumiConfig.StackName },
        { "managedBy", "iac" },
    };
}