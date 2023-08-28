using CadProjectorViewer.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MonchaSDKTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SerializiableTest()
        {
            AppMainModel model = new AppMainModel();
            model.SaveSceneCommand.Execute(null);
        }
    }
}
