//
//  VungleBanner.h
//  Vungle Unity Plugin 6.7.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#ifndef VungleBanner_h
#define VungleBanner_h

#import "VungleManager.h"

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
    @public int marginLeft;
    @public int marginTop;
    @public int marginRight;
    @public int marginBottom;
    @public int bannerSize;
    @public int bannerPosition;
    @public NSString *placementID;
    UIView *adViewContainer;
}

- (BOOL)loadBanner:(NSError **)error;
- (BOOL)showBanner:(NSError **)error;
- (void)closeBanner;
+ (VungleAdSize) getVungleBannerSize:(int)adSizeIndex;

@end
#endif /* VungleBanner_h */
