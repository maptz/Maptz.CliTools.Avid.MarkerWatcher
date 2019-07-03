using Moq;
using System;
using Xunit;
using Maptz.Testing;

namespace Maptz.CliTools.Avid.MarkerWatcher.Tool.Tests
{

    public interface IThingDependency
    {
        /* #region Public Properties */
        int Meaning { get; set; }
        /* #endregion Public Properties */
        /* #region Public Methods */
        string JoinUpper(string v1, string v2);
        /* #endregion Public Methods */
    }

    public class MockingTest1
    {
        /* #region Public Methods */
        public void Test1()
        {
            /* #region Arrange */
            // create mock version
            var mockDependency = new Mock<IThingDependency>();
            // set up mock version's method
            mockDependency.Setup(x => x.JoinUpper(It.IsAny<string>(), It.IsAny<string>()))
                      .Returns("A B");
            // set up mock version's property
            mockDependency.Setup(x => x.Meaning)
                      .Returns(42);

            //Setup object
            //TODO: Inject mockDependency.Object
            /* #endregion*/


            /* #region Act */
            //TODO: ACT
            /* #endregion*/


            /* #region Assert */
            // Assert that the JoinUpper method was called with Sarah Smith
            mockDependency.Verify(x => x.JoinUpper("Sarah", "Smith"), Times.Once);

            // Assert that the Meaning property was accessed once
            mockDependency.Verify(x => x.Meaning, Times.Once);
            /* #endregion*/



        }
        /* #endregion Public Methods */
    }
}