#import <SafariServices/SafariServices.h>

extern UIViewController * UnityGetGLViewController();

extern "C"
{
  void launchUrl(const char * url)
  {
    // Get the instance of ViewController that Unity is displaying now
    UIViewController * uvc = UnityGetGLViewController();
    // Generate an NSURL object based on the C string passed from C#
    NSURL * URL = [NSURL URLWithString: [[NSString alloc] initWithUTF8String:url]];
    // Create an SFSafariViewController object from the generated URL
    SFSafariViewController * sfvc = [[SFSafariViewController alloc] initWithURL:URL];
    //Start the generated SFSafariViewController object
    [uvc presentViewController:sfvc animated:YES completion:nil];
  }

  void dismiss()
  {
    UIViewController * uvc = UnityGetGLViewController();
    [uvc dismissViewControllerAnimated:YES completion:nil];
  }
}