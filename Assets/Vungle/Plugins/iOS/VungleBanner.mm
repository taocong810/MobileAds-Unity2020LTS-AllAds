//
//  VungleBanner.mm
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "VungleBanner.h"
#import <VungleSDK/VungleSDK.h>
#import "VungleUtility.h"
#import "VungleSDKDelegate.h"

@interface VungleBanner ()

@property (readwrite, assign) BOOL muteIsSet;
@property (readwrite, assign) BOOL muted;

@end

@implementation VungleBanner

#pragma mark - Banner interface

- (instancetype)initWithPlacement:(NSString *)placementID size:(int)size position:(int)position viewController:(UIViewController *)viewController {
    self->bannerSize = size;
    self->bannerPosition = position;
    self->placementID = placementID;
    self->viewController = viewController;
    return self;
}

- (void)loadBanner {
    // Check if requested size is "MREC" or not. If MREC, `adSize` is not necessary.
    NSError *error;
    BOOL result;
    if (bannerSize != VunglePluginAdSizeBannerMrec) {
        VungleAdSize requestedAdSize = [VungleBanner getVungleBannerSize:bannerSize];
        if ([[VungleSDK sharedSDK] isAdCachedForPlacementID:placementID withSize:requestedAdSize]) {
            [[VungleSDKDelegate instance] vungleAdPlayabilityUpdate:YES placementID:placementID error:nil];
            return;
        }
        result = [[VungleSDK sharedSDK] loadPlacementWithID:placementID withSize:requestedAdSize error:&error];
    } else {
        if ([[VungleSDK sharedSDK] isAdCachedForPlacementID:placementID]) {
            [[VungleSDKDelegate instance] vungleAdPlayabilityUpdate:YES placementID:placementID error:nil];
            return;
        }
        result = [[VungleSDK sharedSDK] loadPlacementWithID:placementID error:&error];
    }
    if (!result) {
        if (error) {
            if (error.code == VungleSDKResetPlacementForDifferentAdSize) {
                // This is fine for banners. The placement will auto-reload the new size
                return;
            }
            NSString *message = [NSString stringWithFormat:@"Vungle: Failed to request banner %@ - %@", placementID, [error localizedDescription]];
            [VungleUtility sendErrorMessage:message];
        } else {
            NSString *message = [NSString stringWithFormat:@"Vungle: Failed to request banner %@", placementID];
            [VungleUtility sendErrorMessage:message];
        }
    }
}

- (void)showBanner:(NSString *)options {
    if (adViewContainer) {
        // already showing banner so return success
        return;
    }

    [self createViewContainer];
    [viewController.view addSubview:adViewContainer];
    [viewController.view bringSubviewToFront:adViewContainer];
    [self positionViewContainer];

    NSMutableDictionary* playOptions = [self parseOptions:options];
    NSError *error;
    BOOL succeed = [VungleSDK.sharedSDK addAdViewToView:adViewContainer withOptions:playOptions placementID:placementID error:&error];
    if (!succeed) {
        [VungleUtility sendErrorMessage:[NSString stringWithFormat:@"Vungle: Failed to show banner %@ with error %@", placementID, [error localizedDescription]]];
        [self closeBanner];
    } else {
        // Need to pause the audio session or there will be overlapping audio for MREC
        if (bannerSize == VunglePluginAdSizeBannerMrec) {
            if (self.muteIsSet && self.muted) {
                NSLog(@"Vungle: Pausing audio session");
                [VungleUtility pauseAudioSession];
            }
        }
    }
}

- (NSMutableDictionary *)parseOptions:(NSString *)options {
    NSMutableDictionary *opt = [NSMutableDictionary dictionary];
    NSError *error;
    if (options.length) {
        NSData *data = [NSData dataWithBytes:options.UTF8String length:options.length];
        NSDictionary *json = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:&error];
        if ([json objectForKey:@"muted"]) {
            self.muteIsSet = YES;
            self.muted = [[json objectForKey:@"muted"] boolValue];
            opt[VunglePlayAdOptionKeyStartMuted] = @(self.muted);
        }
    }
    return opt;
}

- (void)closeBanner {
    NSLog(@"Vungle: Reverting audio session");
    [[VungleSDK sharedSDK] finishDisplayingAd:placementID];
    [VungleUtility revertAudioSession];
    [adViewContainer removeFromSuperview];
}

#pragma mark - Conversion methods
+ (VungleAdSize) getVungleBannerSize:(int)adSizeIndex {
    VungleAdSize adSize;
    switch (adSizeIndex) {
        case VunglePluginAdSizeBanner:
            adSize = VungleAdSizeBanner;
            break;
        case VunglePluginAdSizeBannerShort:
            adSize = VungleAdSizeBannerShort;
            break;
        case VunglePluginAdSizeBannerLeaderboard:
            adSize = VungleAdSizeBannerLeaderboard;
            break;
        case VunglePluginAdSizeUnknown:
        default:
            [VungleUtility sendWarningMessage:@"Vungle: Unknown banner size provided."];
            adSize = VungleAdSizeUnknown;
            break;

    }
    return adSize;
}

- (VungleBannerPosition) getVungleBannerPosition:(int)adSizeIndex {
    enum VungleBannerPosition position;
    switch (adSizeIndex) {
        case 0:
            position = TopLeft;
            break;
        case 1:
            position = TopCenter;
            break;
        case 2:
            position = TopRight;
            break;
        case 3:
            position = Centered;
            break;
        case 4:
            position = BottomLeft;
            break;
        case 5:
            position = BottomCenter;
            break;
        case 6:
            position = BottomRight;
            break;
        default:
            [VungleUtility sendWarningMessage:@"Vungle: Unknown banner position provided."];
            position = Unknown;
            break;
    }
    return position;
}

- (CGSize)convertVunglePluginAdSizeToCGSize:(VunglePluginAdSize)size {
    CGSize returnSize;
    switch (size) {
        case VunglePluginAdSizeUnknown:
            returnSize = CGSizeZero;
            break;
        case VunglePluginAdSizeBanner:
            returnSize = kVNGPluginAdSizeBanner;
            break;
        case VunglePluginAdSizeBannerShort:
            returnSize = kVNGPluginAdSizeBannerShort;
            break;
        case VunglePluginAdSizeBannerMrec:
            returnSize = kVNGPluginAdSizeMediumRectangle;
            break;
        case VunglePluginAdSizeBannerLeaderboard:
            returnSize = kVNGPluginAdSizeLeaderboard;
            break;
        default:
            [VungleUtility sendErrorMessage:@"Vungle: Invalid ad size provided."];
            returnSize = CGSizeZero;
            break;
    }
    return returnSize;
}

#pragma mark - Positioning methods

- (void)createViewContainer {
    CGSize adSize = [self convertVunglePluginAdSizeToCGSize:(VunglePluginAdSize)bannerSize];
    if (adViewContainer) {
        // same placement was requested
        // if there is already an ad view, remove it. it might be a different size
        [adViewContainer removeFromSuperview];
        adViewContainer = nil;
    }
    adViewContainer = [[UIView alloc] initWithFrame:CGRectMake(0, 0, adSize.width, adSize.height)];
}

- (void)positionViewContainer {
    VungleBannerPosition adPosition = [self getVungleBannerPosition:bannerPosition];
    CGFloat screenWidth = [UIScreen mainScreen].bounds.size.width;
    CGFloat screenHeight = [UIScreen mainScreen].bounds.size.height;

    // iOS 11 APIs
    if (@available(iOS 11.0, *)) {
        UIView* superview = adViewContainer.superview;
        adViewContainer.translatesAutoresizingMaskIntoConstraints = NO;
        NSMutableArray<NSLayoutConstraint*>* constraints = [NSMutableArray arrayWithArray:@[
            [adViewContainer.widthAnchor constraintEqualToConstant:CGRectGetWidth(adViewContainer.frame)],
            [adViewContainer.heightAnchor constraintEqualToConstant:CGRectGetHeight(adViewContainer.frame)],
        ]];
        if (firstConstraint && secondConstraint) {
            [NSLayoutConstraint deactivateConstraints:@[firstConstraint, secondConstraint]];
        }
        switch (adPosition) {
            case TopLeft:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.leftAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.leftAnchor constant: x < 0 ? 0 : x],
                    secondConstraint = [adViewContainer.topAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.topAnchor constant:y < 0 ? 0 : y]]];
                break;
            case TopCenter:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.centerXAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerXAnchor constant:x],
                    secondConstraint = [adViewContainer.topAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.topAnchor constant:y < 0 ? 0 : y]]];
                break;
            case TopRight:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.rightAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.rightAnchor constant:x < 0 ? x : 0],
                    secondConstraint = [adViewContainer.topAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.topAnchor constant:y < 0 ? 0 : y]]];
                break;
            case Centered:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.centerXAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerXAnchor constant:x],
                    secondConstraint = [adViewContainer.centerYAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerYAnchor constant:y]]];
                break;
            case BottomLeft:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.leftAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.leftAnchor constant: x < 0 ? 0 : x],
                    secondConstraint = [adViewContainer.bottomAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.bottomAnchor constant:y < 0 ? y : 0]]];
                break;
            case BottomCenter:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.centerXAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerXAnchor constant:x],
                    secondConstraint = [adViewContainer.bottomAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.bottomAnchor constant:y < 0 ? y : 0]]];
                break;
            case BottomRight:
                [constraints addObjectsFromArray:@[
                    firstConstraint = [adViewContainer.rightAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.rightAnchor constant:x < 0 ? x : 0],
                    secondConstraint = [adViewContainer.bottomAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.bottomAnchor constant:y < 0 ? y : 0]]];
                break;
            default:
                NSLog(@"An ad position is not specified.");
                break;
        }
        [NSLayoutConstraint activateConstraints:constraints];
    } else {
        CGRect viewContainerFrame = adViewContainer.frame;

        switch(adPosition) {
            case TopLeft:
                viewContainerFrame.origin.x = x < 0 ? 0 : x;
                viewContainerFrame.origin.y = y < 0 ? 0 : y;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case TopCenter:
                viewContainerFrame.origin.x = (screenWidth / 2) - (viewContainerFrame.size.width / 2) + x;
                viewContainerFrame.origin.y = y < 0 ? 0 : y;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case TopRight:
                viewContainerFrame.origin.x = screenWidth - viewContainerFrame.size.width + x < 0 ? x : 0;
                viewContainerFrame.origin.y = y < 0 ? 0 : y;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case Centered:
                viewContainerFrame.origin.x = (screenWidth / 2) - (viewContainerFrame.size.width / 2) + x;
                viewContainerFrame.origin.y = (screenHeight / 2) - (viewContainerFrame.size.height / 2) + y;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleTopMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case BottomLeft:
                viewContainerFrame.origin.x = x < 0 ? 0 : x;
                viewContainerFrame.origin.y = screenHeight - viewContainerFrame.size.height + y < 0 ? y : 0;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin);
                break;
            case BottomCenter:
                viewContainerFrame.origin.x = (screenWidth / 2) - (viewContainerFrame.size.width / 2) + x;
                viewContainerFrame.origin.y = screenHeight - viewContainerFrame.size.height + y < 0 ? y : 0;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin);
                break;
            case BottomRight:
                viewContainerFrame.origin.x = screenWidth - viewContainerFrame.size.width + x < 0 ? x : 0;
                viewContainerFrame.origin.y = screenHeight - viewContainerFrame.size.height + y < 0 ? y : 0;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleTopMargin);
                break;
            default:
                NSLog(@"Vungle: An ad position is not specified.");
                break;
        }
        adViewContainer.frame = viewContainerFrame;
    }
}

- (void)setOffset:(int)x y:(int)y {
    self->x = x;
    self->y = y;
    if (adViewContainer) {
        [self positionViewContainer];
    }
}

@end
