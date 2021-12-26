using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Tests.TestUtils;
using Xunit;

namespace TheGame.Tests.DevTests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.DevTest)]
  public class GameDbContextDevTests
  {
    [Fact]
    public void CanConnect()
    {
      // TODO implement
    }
  }
}
