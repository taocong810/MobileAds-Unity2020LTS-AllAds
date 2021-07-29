//
//  VungleManager.mm
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import "VungleBanner.h"
#import "VungleManager.h"
#import "VungleUtility.h"
#import <VungleSDK/VungleSDK.h>

@interface VungleManager() {
    // holds a reference to all instances of banners
    // limitation is that it can only hold 1 banner object per placement
    NSMutableDictionary *bannerViewDict;
}
@end

@implementation VungleManager

static VungleManager * _instance = nil;
static dispatch_once_t onceToken;

+ (VungleManager *)instance {
    dispatch_once(&onceToken, ^{
        _instance = [[VungleManager alloc] init];
    });
  return _instance;
}

-(id)init {
    if (self = [super init]) {
        bannerViewDict = [[NSMutableDictionary alloc] init];
    }
    return self;
}

- (BOOL)isLoadedAsBanner:(NSString*)placementID {
    VungleBanner *bannerInfo = bannerViewDict[placementID];
    // MREC don't count as banners, even though it is loaded, played and closed as one
    // Used for determining audio session mute
    return bannerInfo && bannerInfo->bannerSize != VunglePluginAdSizeBannerMrec;
}

- (BOOL)isBannerAvailable:(NSString *)placementID withSize:(int)size {
    VungleBanner *bannerInfo = bannerViewDict[placementID];
    if (bannerInfo) {
        return [[VungleSDK sharedSDK] isAdCachedForPlacementID:placementID withSize:[VungleBanner getVungleBannerSize:size]];
    } else {
        return false;
    }
}

- (void)requestBanner:(NSString *)placementID withSize:(int)bannerSize atPosition:(int)bannerPosition {
    VungleBanner *bannerInfo = bannerViewDict[placementID];
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
        bannerViewDict[placementID] = bannerInfo;
    } else {
        if (error) {
            if (error.code == VungleSDKResetPlacementForDifferentAdSize) {
                // This is fine for banners. The placement will auto-reload the new size
                bannerViewDict[placementID] = bannerInfo;
            }
            NSString *message = [NSString stringWithFormat:@"Failed to request banner %@ - %@",
                                 placementID, [error localizedDescription]];
            [VungleUtility sendErrorMessage:message];
        }
    }
}

- (void)setOffset:(NSString *)placementID x:(int)x y:(int)y {
    VungleBanner *bannerInfo = bannerViewDict[placementID];
    if (bannerInfo) {
        [bannerInfo setOffset:x y:y];
    } else {
        NSString *message = [NSString stringWithFormat:@"Failed to set offset; banner %@ not loaded", placementID];
        [VungleUtility sendErrorMessage:message];
    }
}

- (void)showBanner:(NSString *)placementID {
    VungleBanner *bannerInfo = bannerViewDict[placementID];
    NSError * error;
    if (bannerInfo) {
        BOOL success = [bannerInfo showBanner:&error];
        if (!success) {
            [VungleUtility sendErrorMessage:[NSString stringWithFormat:@"There is an error to displaying banner ad for a placement: %@", placementID]];

            if (error) {
                NSString *message = [NSString stringWithFormat:@"Failed to show banner %@ with error - %@",
                                     placementID, [error localizedDescription]];
                [VungleUtility sendErrorMessage:message];
            }
        }
    } else {
        NSString *message = [NSString stringWithFormat:@"Failed to show banner; banner %@ not loaded", placementID];
        [VungleUtility sendErrorMessage:message];
    }
}

- (void)closeBanner:(NSString *)placementID {
    VungleBanner *bannerInfo = bannerViewDict[placementID];
    if (bannerInfo) {
        [bannerInfo closeBanner];
        [[VungleSDK sharedSDK] finishDisplayingAd:placementID];
        [bannerViewDict removeObjectForKey:placementID];
    } else {
        NSString *message = [NSString stringWithFormat:@"Failed to close banner; banner %@ not found", placementID];
        [VungleUtility sendErrorMessage:message];
    }
}

@end
