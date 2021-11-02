//
//  VungleBanner.h
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//
//  This class manages creating the UIView for the the banner / MREC,
//  positioning the UIView and destroying it. The instances are managed by
//  VungleManager
//

#ifndef VungleBanner_h
#define VungleBanner_h

#import <VungleSDK/VungleSDK.h>

typedef NS_ENUM (NSInteger, VungleBannerPosition) {
    TopLeft = 0,
    TopCenter,
    TopRight,
    Centered,
    BottomLeft,
    BottomCenter,
    BottomRight,
    Unknown
};

// This enum should match the order and value of Unity's enum
typedef NS_ENUM (NSInteger, VunglePluginAdSize) {
    VunglePluginAdSizeBanner = 0,                 // width = 320.0f, .height = 50.0f
    VunglePluginAdSizeBannerShort,                // width = 300.0f, .height = 50.0f
    VunglePluginAdSizeBannerMrec,                 // width = 300.0f, .height = 250.0f
    VunglePluginAdSizeBannerLeaderboard,          // width = 728.0f, .height = 90.0f
    VunglePluginAdSizeUnknown
};

static CGSize const kVNGPluginAdSizeBanner = { .width = 320.0f, .height = 50.0f };
static CGSize const kVNGPluginAdSizeBannerShort = { .width = 300.0f, .height = 50.0f };
static CGSize const kVNGPluginAdSizeMediumRectangle = { .width = 300.0f, .height = 250.0f };
static CGSize const kVNGPluginAdSizeLeaderboard = { .width = 728.0f, .height = 90.0f };

@interface VungleBanner : NSObject {
    @public int x;
    @public int y;
    @public int bannerSize;
    @public int bannerPosition;
    @public NSString *placementID;
    UIView *adViewContainer;
    UIViewController *viewController;
    NSLayoutConstraint* firstConstraint;
    NSLayoutConstraint* secondConstraint;
}

- (instancetype)init NS_UNAVAILABLE;
- (instancetype)initWithPlacement:(NSString *)placementID size:(int)size position:(int)position viewController:(UIViewController *)viewController;
- (void)loadBanner;
- (void)setOffset:(int)x y:(int)y;
- (void)showBanner:(NSString *)options;
- (void)closeBanner;
+ (VungleAdSize) getVungleBannerSize:(int)adSizeIndex;

@end
#endif /* VungleBanner_h */
