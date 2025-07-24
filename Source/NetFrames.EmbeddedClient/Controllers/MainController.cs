using NetFrames.EmbeddedClient.Contracts;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient.Controllers;

public class MainController
{
    private IGalleryViewerHardware hardware;

    private DisplayController displayController;
    //private InputController inputController;

    private int selectedIndex = 0;

    public MainController() { }

    public Task Initialize(IGalleryViewerHardware hardware)
    {
        this.hardware = hardware;

        //inputController = new InputController(hardware);
        //inputController.LeftButtonPressed += LeftButtonPressed;
        //inputController.RightButtonPressed += RightButtonPressed;

        displayController = new DisplayController(
            this.hardware.Display,
            this.hardware.DisplayRotation);

        return Task.CompletedTask;
    }

    //private void LeftButtonPressed(object sender, EventArgs e)
    //{
    //    if (selectedIndex + 1 > 2)
    //        selectedIndex = 0;
    //    else
    //        selectedIndex++;

    //    displayController.UpdateDisplay(selectedIndex);
    //}

    //private void RightButtonPressed(object sender, EventArgs e)
    //{
    //    if (selectedIndex - 1 < 0)
    //        selectedIndex = 2;
    //    else
    //        selectedIndex--;

    //    displayController.UpdateDisplay(selectedIndex);
    //}
}
