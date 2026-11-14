#import <Foundation/Foundation.h>

// D-021-AC-07: the name of the app's receipt file, "sandboxReceipt" in the App Store sandbox (TestFlight, Ad Hoc and
// Xcode builds) and "receipt" for the App Store, so a test purchase is told from a real one. Unity frees the string.
const char *TreadShredReceiptFileName(void)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    NSString *name = NSBundle.mainBundle.appStoreReceiptURL.lastPathComponent ?: @"";
#pragma clang diagnostic pop
    return strdup(name.UTF8String);
}
