//
//  VungleManager.mm
//  Vungle Unity Plugin 6.7.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import "VungleManager.h"
#import <objc/runtime.h>

#if UNITY_VERSION < 500
void UnityPause(bool pause);
#else
void UnityPause(int pause);
#endif

void UnitySendMessage(const char * className, const char * methodName, const char * param);

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

#pragma mark - VGVunglePubDelegate

- (void)vungleWillShowAdForPlacementID:(nullable NSString *)placementID {
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
    UnitySendMessage("VungleManager", "OnAdEnd", [VungleManager jsonFromObject:dict].UTF8String);
}

- (void)vungleSDKDidInitialize {
    UnitySendMessage("VungleManager", "OnInitialize", "");
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
