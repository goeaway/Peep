using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.BrowserAdapter;
using Peep.PageActions;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Core - Crawler - Page Action Performer")]
    public class PageActionPerformerTests
    {
        [TestMethod]
        public async Task Perform_Throws_If_BrowserAdapter_Null()
        {
            var pageAction = new SerialisablePageAction();
            var performer = new PageActionPerformer();
            
            await Assert
                .ThrowsExceptionAsync<ArgumentNullException>(
                    () => performer.Perform(pageAction, null));
        }

        [TestMethod]
        public async Task Perform_Throws_If_PageAction_Null()
        {
            var adapterMock = new Mock<IBrowserAdapter>();
            var performer = new PageActionPerformer();
            
            await Assert
                .ThrowsExceptionAsync<ArgumentNullException>(
                    () => performer.Perform(null, adapterMock.Object));
        }

        [TestMethod]
        public async Task Perform_Calls_BrowserAdapter_WaitForSelector_For_Wait_Type()
        {
            const string SELECTOR = "selector";
            const SerialisablePageActionType TYPE = SerialisablePageActionType.Wait;
            
            var adapterMock = new Mock<IBrowserAdapter>();
            var pageAction = new SerialisablePageAction
            {
                Value = SELECTOR,
                Type = TYPE
            };
            var performer = new PageActionPerformer();

            await performer.Perform(pageAction, adapterMock.Object);
            
            adapterMock
                .Verify(
                    mock => mock
                        .WaitForSelector(
                            SELECTOR, 
                            It.Is<TimeSpan>(value => value == TimeSpan.FromSeconds(30))));
        }
        
        [TestMethod]
        public async Task Perform_Calls_BrowserAdapter_Click_For_Click_Type()
        {
            const string SELECTOR = "selector";
            const SerialisablePageActionType TYPE = SerialisablePageActionType.Click;
            
            var adapterMock = new Mock<IBrowserAdapter>();
            var pageAction = new SerialisablePageAction
            {
                Value = SELECTOR,
                Type = TYPE
            };
            var performer = new PageActionPerformer();

            await performer.Perform(pageAction, adapterMock.Object);
            
            adapterMock
                .Verify(
                    mock => mock
                        .Click(
                            SELECTOR));
        }
        
        [TestMethod]
        public async Task Perform_Calls_BrowserAdapter_Scroll_For_Scroll_Type()
        {
            const int SCROLL_AMOUNT = 1;
            const SerialisablePageActionType TYPE = SerialisablePageActionType.Scroll;
            
            var adapterMock = new Mock<IBrowserAdapter>();
            var pageAction = new SerialisablePageAction
            {
                Value = SCROLL_AMOUNT,
                Type = TYPE
            };
            var performer = new PageActionPerformer();

            await performer.Perform(pageAction, adapterMock.Object);
            
            adapterMock
                .Verify(
                    mock => mock
                        .ScrollY(
                            SCROLL_AMOUNT));
        }
    }
}