using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.PlateTrainer.Training;

namespace TheGame.Tests.MlTrainer;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class DataLoaderTests
{
  [Fact]
  public void WillGenerateCarteseanProduct()
  {
    var variantsParts = new string[][]
    {
      [ "a", "b" ],
      [ "c" ],
      [ "d", "e" ],
    };

    var actualCombinations = DataLoader.CombineAsCarteseanProduct(variantsParts)
      .ToArray();

    Assert.Collection(actualCombinations,
      item => Assert.Equal("a c d", item),
      item => Assert.Equal("a c e", item),
      item => Assert.Equal("b c d", item),
      item => Assert.Equal("b c e", item)
    );
  }

  [Fact]
  public void WillCreateDescriptionCombinations()
  {
    var description = new Dictionary<string, string?>()
    {
      { "plate", "solid white" }
    };

    var synonyms = new Dictionary<string, string[]>()
    {
      { "solid", [ "all" ] },
      { "plate", [ "background" ] },
    };

    var actualCombinations = DataLoader.CreateFeatureTextCombinations(description, synonyms)
      .ToArray();

    Assert.Collection(actualCombinations,
      item => Assert.Equal("background all white", item),
      item => Assert.Equal("all white background", item),
      item => Assert.Equal("plate all white", item),
      item => Assert.Equal("all white plate", item),
      item => Assert.Equal("all white", item),

      item => Assert.Equal("background solid white", item),
      item => Assert.Equal("solid white background", item),
      item => Assert.Equal("plate solid white", item),
      item => Assert.Equal("solid white plate", item),
      item => Assert.Equal("solid white", item)
    );
  }
}
