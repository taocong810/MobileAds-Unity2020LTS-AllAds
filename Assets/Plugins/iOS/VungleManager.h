//
//  VungleManager.h
//  Vungle Unity Plugin 6.8.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <VungleSDK/VungleSDK.h>
#import <VungleSDK/VungleSDKHeaderBidding.h>
#import <VungleSDK/VungleSDKCreativeTracking.h>
#import "VungleBanner.h"

NS_ASSUME_NONNULL_BEGIN

@interface VungleManager : NSObject <VungleSDKDelegate, VungleSDKHeaderBidding, VungleSDKCreativeTracking, VungleSDKLogger>

+ (VungleManager*)sharedManager;
+ (id)objectFromJson:(NSString*)json;
+ (NSString*)jsonFromObject:(id)object;

- (void)initSDK:(NSString *)appId pluginName:(NSString *)pluginVersion headerBidding:(BOOL)initHeaderBiddingDelegate;
- (BOOL)isBannerAvailable:(NSString *)placementID withSize:(int)size;
- (void)requestBanner:(NSString *)placementID withSize:(int)bannerSize atPosition:(int)bannerPosition;
- (void)setMargins:(NSString *)placementID marginLeft:(int)marginLeft marginTop:(int)marginTop marginRight:(int)marginRight marginBottom:(int)marginBottom;
- (void)showBanner:(NSString *)placementID;
- (void)closeBanner:(NSString *)placementID;
- (void)requestTrackingAuthorization;

@end
NS_ASSUME_NONNULL_END
