//
//  VungleManager.mm
//  Vungle Unity Plugin 6.8.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import "UnityInterface.h"
#import "VungleManager.h"
#import <objc/runtime.h>
#import <StoreKit/SKAdNetwork.h>
#import <AVFoundation/AVAudioSession.h>

#if defined(__IPHONE_14_0) && __IPHONE_OS_VERSION_MAX_ALLOWED >= __IPHONE_14_0
#import <AppTrackingTransparency/ATTrackingManager.h>
#endif

#if __has_feature(objc_arc)
#define SAFE_ARC_AUTORELEASE(x) (x)
#else
#define SAFE_ARC_AUTORELEASE(x) ([(x) autorelease])
#endif

@interface VungleManager ()
@property (nonatomic, strong) NSMutableDictionary *bannerViewDict;
@end

@implementation VungleManager

static VungleManager *sharedSingleton = nil;
static dispatch_once_t onceToken;
AVAudioSessionCategory audioCategory;
BOOL categoryIsSet = NO;

#pragma mark Class Methods

+ (VungleManager*)sharedManager {
    dispatch_once(&onceToken, ^{
        sharedSingleton = [[VungleManager alloc] init];
        [sharedSingleton setupManager];
    });

    return sharedSingleton;
}

+ (NSString*)jsonFromObject:(id)object {
    NSError *error = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:object options:0 error:&error];

    if (jsonData && !error) {
        return SAFE_ARC_AUTORELEASE([[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding]);
    }

    if (error) {
        NSString *message = [NSString stringWithFormat:@"Failed to create JSON - %@",
                             [error localizedDescription]];
        UnitySendMessage("VungleManager", "OnError", [message UTF8String]);
    }

    return @"{}";
}

#pragma mark - Public

+ (id)objectFromJson:(NSString*)json {
    NSError *error = nil;
    NSData *data = [NSData dataWithBytes:json.UTF8String length:json.length];
    NSObject *object = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:&error];
    if (error) {
        NSString *message = [NSString stringWithFormat:@"Failed to create object from JSON - %@",
                             [error localizedDescription]];
        UnitySendMessage("VungleManager", "OnError", [message UTF8String]);
    }

    return object;
}

#pragma mark - SDK init

- (void)initSDK:(NSString *)appId pluginName:(NSString *)pluginVersion headerBidding:(BOOL)initHeaderBiddingDelegate {
    if (![VungleSDK sharedSDK].initialized) {
        if ([[VungleSDK sharedSDK] respondsToSelector:@selector(setPluginName:version:)]) {
            [[VungleSDK sharedSDK] performSelector:@selector(setPluginName:version:) withObject:@"unity" withObject:pluginVersion];
        }

        [VungleSDK sharedSDK].delegate = self;
        [[VungleSDK sharedSDK] setLoggingEnabled:true];
        [[VungleSDK sharedSDK] attachLogger:self];
        [VungleSDK sharedSDK].creativeTrackingDelegate = self;
        if (initHeaderBiddingDelegate) {
            [VungleSDK sharedSDK].headerBiddingDelegate = self;
        }

        NSError * error;
        if (![[VungleSDK sharedSDK] startWithAppId:appId error:&error]) {
            NSString *message = [NSString stringWithFormat:@"Failed to initialize SDK - %@",
                                 [error localizedDescription]];
            [self vungleErrorLog:message];
        }
    }
}

#pragma mark - AdLifecycle-Banner

- (void)setupManager {
    self.bannerViewDict = [[NSMutableDictionary alloc] init];
}

- (BOOL)isBannerAvailable:(NSString *)placementID withSize:(int)size {
    VungleBanner *bannerInfo = self.bannerViewDict[placementID];
    if (bannerInfo) {
        return [[VungleSDK sharedSDK] isAdCachedForPlacementID:placementID withSize:[VungleBanner getVungleBannerSize:size]];
    } else {
        return false;
    }
}

- (void)requestBanner:(NSString *)placementID withSize:(int)bannerSize atPosition:(int)bannerPosition {
    VungleBanner *bannerInfo = self.bannerViewDict[placementID];
    if (!bannerInfo) {
        bannerInfo = [[VungleBanner alloc] init];
        bannerInfo->placementID = placementID;
    }
    // if the placement already has been loaded, update the size and position
    // then try to load the assets for the new size
    bannerInfo->bannerSize = bannerSize;
    bannerInfo->bannerPosition = bannerPosition;

    NSError *error;
    BOOL success = [bannerInfo loadBanner:&error];
    if (success) {
        self.bannerViewDict[placementID] = bannerInfo;
    } else {
        if (error) {
            if (error.code == VungleSDKResetPlacementForDifferentAdSize) {
                // This is fine for banners. The placement will auto-reload the new size
                self.bannerViewDict[placementID] = bannerInfo;
            }
            NSString *message = [NSString stringWithFormat:@"Failed to request banner %@ - %@",
                                 placementID, [error localizedDescription]];
            [self vungleErrorLog:message];
        }
    }
}

- (void)setMargins:(NSString *)placementID marginLeft:(int)marginLeft marginTop:(int)marginTop marginRight:(int)marginRight marginBottom:(int)marginBottom {
    VungleBanner *bannerInfo = self.bannerViewDict[placementID];
    if (bannerInfo) {
        bannerInfo->marginLeft = marginLeft;
        bannerInfo->marginTop = marginTop;
        bannerInfo->marginRight = marginRight;
        bannerInfo->marginBottom = marginBottom;
    } else {
        NSString *message = [NSString stringWithFormat:@"Failed to set margins; banner %@ not loaded", placementID];
        [self vungleErrorLog:message];
    }
}

- (void)showBanner:(NSString *)placementID {
    VungleBanner *bannerInfo = self.bannerViewDict[placementID];
    NSError * error;
    if (bannerInfo) {
        BOOL success = [bannerInfo showBanner:&error];
        if (!success) {
            [self vungleErrorLog:[NSString stringWithFormat:@"There is an error to displaying banner ad for a placement: %@", placementID]];

            if (error) {
                NSString *message = [NSString stringWithFormat:@"Failed to show banner %@ with error - %@",
                                     placementID, [error localizedDescription]];
                [self vungleErrorLog:message];
            }
        }
    } else {
        NSString *message = [NSString stringWithFormat:@"Failed to show banner; banner %@ not loaded", placementID];
        [self vungleErrorLog:message];
    }
}

- (void)closeBanner:(NSString *)placementID {
    VungleBanner *bannerInfo = self.bannerViewDict[placementID];
    if (bannerInfo) {
        [bannerInfo closeBanner];
        [[VungleSDK sharedSDK] finishDisplayingAd:placementID];
        [self.bannerViewDict removeObjectForKey:placementID];
    } else {
        NSString *message = [NSString stringWithFormat:@"Failed to close banner; banner %@ not found", placementID];
        [self vungleErrorLog:message];
    }
}

/*
  0 - Not Determined
  1 - Restricted
  2 - Denied
  3 - Authorized
*/
- (void)requestTrackingAuthorization {
    if (@available(iOS 14, *)) {
#if defined(__IPHONE_14_0) && __IPHONE_OS_VERSION_MAX_ALLOWED >= __IPHONE_14_0
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            UnitySendMessage("VungleManager", "TrackingCallback", [[@(status) stringValue] UTF8String]);
        }];
#endif
    } else {
        // Assume good to go since not iOS 14?
        UnitySendMessage("VungleManager", "TrackingCallback", "3");
    };
}

#pragma mark - VGVunglePubDelegate

- (void)vungleWillShowAdForPlacementID:(nullable NSString *)placementID {
    VungleBanner *bannerInfo = self.bannerViewDict[placementID];
    // if not a banner or if the banner is MREC, then
    // mute background if the SDK is not muted
    if ((!bannerInfo || bannerInfo->bannerSize == VunglePluginAdSizeBannerMrec) &&
        ![VungleSDK sharedSDK].muted) {
        UnitySetAudioSessionActive(FALSE);
        categoryIsSet = YES;
        audioCategory = [[AVAudioSession sharedInstance] category];
        [[AVAudioSession sharedInstance] setCategory:AVAudioSessionCategorySoloAmbient error:NULL];
        [[AVAudioSession sharedInstance] setActive:YES withOptions:AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers error:NULL];
    }
    UnitySendMessage("VungleManager", "OnAdStart", placementID? [placementID UTF8String] : "");
}

- (void)vungleAdPlayabilityUpdate:(BOOL)isAdPlayable placementID:(nullable NSString *)placementID error:(nullable NSError *)error {
    NSDictionary *dict = @{
        @"isAdAvailable": [NSNumber numberWithBool:isAdPlayable],
        @"placementID": placementID ?: @"",
        @"error": error ? [error localizedDescription] : @"",
    };
    if (error) {
        [self vungleErrorLog:[NSString stringWithFormat:@"AdPlayability error: %@", [error localizedDescription]]];
    }
    UnitySendMessage("VungleManager", "OnAdPlayable", [VungleManager jsonFromObject:dict].UTF8String);
}

- (void)vungleDidCloseAdWithViewInfo:(nonnull VungleViewInfo *)info placementID:(nonnull NSString *)placementID {
    NSDictionary *dict = @{
        @"completedView": [info completedView] ?: [NSNull null],
        @"playTime": [info playTime] ?: [NSNull null],
        @"didDownload": [info didDownload] ?: [NSNull null],
        @"placementID": placementID ?: @""
    };
    if (categoryIsSet) {
        categoryIsSet = NO;
        [[AVAudioSession sharedInstance] setActive:NO withOptions:AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers error:NULL];
        [[AVAudioSession sharedInstance] setCategory:audioCategory error:NULL];
        UnitySetAudioSessionActive(TRUE);
    }
    UnitySendMessage("VungleManager", "OnAdEnd", [VungleManager jsonFromObject:dict].UTF8String);
}

- (void)vungleSDKDidInitialize {
    UnitySendMessage("VungleManager", "OnInitialize", "1");
}

- (void)vungleSDKFailedToInitializeWithError:(NSError *)error {
    [self vungleErrorLog:[error localizedDescription]];
    UnitySendMessage("VungleManager", "OnInitialize", "0");
}

- (void)vungleSDKLog:(NSString*)message {
    UnitySendMessage("VungleManager", "OnSDKLog", [message UTF8String]);
}

- (void)vungleErrorLog:(NSString*)message {
    UnitySendMessage("VungleManager", "OnError", [message UTF8String]);
}

- (void)placementPrepared:(NSString *)placement withBidToken:(NSString *)bidToken {
    NSDictionary *dict = @{
        @"placementID": placement ?: @"",
        @"bidToken": bidToken ?: @""
    };
    UnitySendMessage("VungleManager", "OnPlacementPrepared", [VungleManager jsonFromObject:dict].UTF8String);
}

- (void)vungleCreative:(nullable NSString *)creativeID readyForPlacement:(nullable NSString *)placementID {
    NSDictionary *dict = @{
        @"placementID": placementID ?: @"",
        @"creativeID": creativeID ?: @""
    };
    UnitySendMessage("VungleManager", "OnVungleCreative", [VungleManager jsonFromObject:dict].UTF8String);
}
@end
