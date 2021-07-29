//
//  VungleManager.h
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//
//  This class manages all the banner instances.
//  If there is a need to manage SDK instances, this is the place to do it
//

#import <Foundation/Foundation.h>

@interface VungleManager : NSObject
{
}
+ (VungleManager*)instance;
- (BOOL)isLoadedAsBanner:(NSString*)placementID;
- (BOOL)isBannerAvailable:(NSString *)placementID withSize:(int)size;
- (void)requestBanner:(NSString *)placementID withSize:(int)bannerSize atPosition:(int)bannerPosition;
- (void)setOffset:(NSString *)placementID x:(int)x y:(int)y;
- (void)showBanner:(NSString *)placementID;
- (void)closeBanner:(NSString *)placementID;
@end
