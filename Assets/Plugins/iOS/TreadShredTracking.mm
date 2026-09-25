#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <UIKit/UIKit.h>

extern "C" void UnitySendMessage(const char *obj, const char *method, const char *msg);

extern "C" void TreadShredRequestTrackingAuthorization(void) {
    if (@available(iOS 14.0, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            (void)status;
            UnitySendMessage("Tank Ad Service", "OnTrackingAuthorizationCompleted", "");
        }];
    } else {
        UnitySendMessage("Tank Ad Service", "OnTrackingAuthorizationCompleted", "");
    }
}
