using FluentSocket.Utils;
using Xunit;

namespace FluentSocket.Tests.Utils
{
    public class FlowControlUtilTest
    {
        [Fact]
        public void CalculateFlowControlTimeMilliseconds_Test()
        {
            var waitMilliseconds1 = FlowControlUtil.CalculateFlowControlTimeMilliseconds(1750, 1000);

            Assert.Equal(75, waitMilliseconds1);

        }
    }
}
