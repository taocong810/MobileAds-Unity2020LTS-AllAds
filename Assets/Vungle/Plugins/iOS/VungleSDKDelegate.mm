//
//  VungleSDKDelegate.mm
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import <Foundation/Foundation.h>

#import "VungleSDKDelegate.h"
#import "VungleUtility.h"
#import <VungleSDK/VungleSDK.h>

extern "C" void UnitySendMessage(const char *, const char *, const char *);

@implementation VungleSDKDelegate

static VungleSDKDelegate * _instance = nil;
static dispatch_once_t onceToken;

+ (VungleSDKDelegate *)instance {
    dispatch_once(&onceToken, ^{
        _instance = [[VungleSDKDelegate alloc] init];
    });
  return _instance;
}

- (void)vungleWillShowAdForPlacementID:(nullable NSString *)placementID {
    UnitySendMessage("VungleManager", "OnAdStart", placementID ? [placementID UTF8String] : "");
}

- (void)vungleAdPlayabilityUpdate:(BOOL)isAdPlayable placementID:(nullable NSString *)placementID error:(nullable NSError *)error {
    NSDictionary *dict = @{
        @"isAdAvailable": [NSNumber numberWithBool:isAdPlayable],
        @"placementID": placementID ?: @"",
        @"error": error ? [error localizedDescription] : @"",
    };
    if (error) {
        [VungleUtility sendErrorMessage:[NSString stringWithFormat:@"AdPlayability error: %@", [error localizedDescription]]];
    }
    UnitySendMessage("VungleManager", "OnAdPlayable", [VungleUtility jsonFromObject:dict].UTF8String);}

- (void)vungleWillCloseAdForPlacementID:(nonnull NSString *)placementID {
    NSDictionary *dict = @{
        @"placementID": placementID ?: @""
    };
    UnitySendMessage("VungleManager", "OnAdEnd", [VungleUtility jsonFromObject:dict].UTF8String);
}

- (void)vungleDidCloseAdWithViewInfo:(nonnull VungleViewInfo *)info placementID:(nonnull NSString *)placementID {
    NSDictionary *dict = @{
        @"completedView": [info completedView] ?: [NSNull null],
        @"playTime": [info playTime] ?: [NSNull null],
        @"didDownload": [info didDownload] ?: [NSNull null],
        @"placementID": placementID ?: @""
    };
    UnitySendMessage("VungleManager", "OnAdEnd", [VungleUtility jsonFromObject:dict].UTF8String);
}

- (void)vungleTrackClickForPlacementID:(nullable NSString *)placementID {
    NSDictionary *dict = @{
        @"placementID": placementID ?: @""
    };
    UnitySendMessage("VungleManager", "OnAdClick", [VungleUtility jsonFromObject:dict].UTF8String);
}

- (void)vungleRewardUserForPlacementID:(nullable NSString *)placementID {
    NSDictionary *dict = @{
        @"placementID": placementID ?: @""
    };
    UnitySendMessage("VungleManager", "OnAdRewarded", [VungleUtility jsonFromObject:dict].UTF8String);
}

- (void)vungleWillLeaveApplicationForPlacementID:(nullable NSString *)placementID {
    NSDictionary *dict = @{
        @"placementID": placementID ?: @""
    };
    UnitySendMessage("VungleManager", "OnAdLeftApplication", [VungleUtility jsonFromObject:dict].UTF8String);
}

- (void)vungleSDKDidInitialize {
    UnitySendMessage("VungleManager", "OnInitialize", "1");
}

// TODO return a more descriptive value for failure that may help determine the cause
// Can use error codes
- (void)vungleSDKFailedToInitializeWithError:(NSError *)error {
    [VungleUtility sendErrorMessage:[error localizedDescription]];
    UnitySendMessage("VungleManager", "OnInitialize", "0");
}

- (void)vungleSDKLog:(NSString*)message {
    [VungleUtility sendLog:message];
}

- (void)vungleErrorLog:(NSString*)message {
    [VungleUtility sendErrorMessage:message];
}

- (void)placementPrepared:(NSString *)placement withBidToken:(NSString *)bidToken {
    NSDictionary *dict = @{
        @"placementID": placement ?: @"",
        @"bidToken": bidToken ?: @""
    };
    UnitySendMessage("VungleManager", "OnPlacementPrepared", [VungleUtility jsonFromObject:dict].UTF8String);
}

- (void)vungleCreative:(nullable NSString *)creativeID readyForPlacement:(nullable NSString *)placementID {
    NSDictionary *dict = @{
        @"placementID": placementID ?: @"",
        @"creativeID": creativeID ?: @""
    };
    UnitySendMessage("VungleManager", "OnVungleCreative", [VungleUtility jsonFromObject:dict].UTF8String);
}

@end
