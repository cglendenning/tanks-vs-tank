#import <StoreKit/StoreKit.h>

// D-023-AC-04: sets the SKAdNetwork conversion value that Apple's install postback carries to AppLovin, with a
// copy to Tenjin (NSAdvertisingAttributionReportEndpoint). StoreKit only: no advertising identifier and no
// tracking prompt (D-005). Values come from SkanConversion in the game's Analytics assembly.
void TreadShredUpdateConversionValue(int fineValue, const char *coarseValue)
{
    void (^done)(NSError *) = ^(NSError *error) {
        if (error != nil)
            NSLog(@"[Attribution] SKAdNetwork conversion value %d not set: %@", fineValue, error);
    };
    if (@available(iOS 16.1, *))
    {
        NSString *coarse = coarseValue != NULL ? [NSString stringWithUTF8String:coarseValue] : @"low";
        SKAdNetworkCoarseConversionValue value = [coarse isEqualToString:@"high"] ? SKAdNetworkCoarseConversionValueHigh
            : [coarse isEqualToString:@"medium"] ? SKAdNetworkCoarseConversionValueMedium
            : SKAdNetworkCoarseConversionValueLow;
        [SKAdNetwork updatePostbackConversionValue:fineValue coarseValue:value lockWindow:NO completionHandler:done];
    }
    else if (@available(iOS 15.4, *))
    {
        [SKAdNetwork updatePostbackConversionValue:fineValue completionHandler:done];
    }
    else
    {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        [SKAdNetwork updateConversionValue:fineValue];
#pragma clang diagnostic pop
    }
}
