//
//  VungleBanner.m
//  Vungle Unity Plugin 6.8.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import "UnityInterface.h"
#import <Foundation/Foundation.h>
#import "VungleBanner.h"

@implementation VungleBanner

#pragma mark - Banner interface
- (BOOL)loadBanner:(NSError **)error {
    // Check if requested size is "MREC" or not. If MREC, `adSize` is not necessary.
    if (bannerSize != VunglePluginAdSizeBannerMrec) {
        VungleAdSize requestedAdSize = [VungleBanner getVungleBannerSize:bannerSize];
        return [[VungleSDK sharedSDK] loadPlacementWithID:placementID withSize:requestedAdSize error:error];
    } else {
        return [[VungleSDK sharedSDK] loadPlacementWithID:placementID error:error];
    }
}

- (BOOL)showBanner:(NSError **)error {
    if (adViewContainer) {
        // already showing banner so return success
        return YES;
    }

    [self createViewContainer];
    [UnityGetGLViewController().view addSubview:adViewContainer];
    [UnityGetGLViewController().view bringSubviewToFront:adViewContainer];
    [self positionViewContainer];

    NSMutableDictionary* playOptions = [[NSMutableDictionary alloc] init];
    playOptions[VunglePlayAdOptionKeyStartMuted] = @([VungleSDK sharedSDK].muted);
    BOOL succeed = [VungleSDK.sharedSDK addAdViewToView:adViewContainer withOptions:playOptions placementID:placementID error:error];
    if (!succeed) {
        UnitySendMessage("VungleManager", "OnSDKLog", [[NSString stringWithFormat:@"Failed to show banner %@ with error %@", placementID, [*error localizedDescription]] UTF8String]);
        [self closeBanner];
    }
    return succeed;
}

- (void)closeBanner {
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
            UnitySendMessage("VungleManager", "OnSDKLog", "Unknown banner size provided.");
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
            UnitySendMessage("VungleManager", "OnSDKLog", "Unknown banner position provided.");
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
            UnitySendMessage("VungleManager", "OnSDKLog", "Invalid ad size provided.");
            returnSize = CGSizeZero;
            break;
    }
    return returnSize;
}

#pragma mark - Positioning methods
- (int)calculateHorizontalMargin:(VungleBannerPosition)adPosition withLeftMargin:(int)leftMargin withRightMargin:(int)rightMargin {
    // There is no such thing as "margins" for iOS native
    // So the implementation would be a horizontal shift
    // Maybe only consider both if it's centered and left if it's left, etc
    int horizontalMargin = leftMargin - rightMargin;

    // TODO: We should calculate margin based on "adPosition" not to display banner UIView outside of screen.
    // Should the plugin lock the UIView to within the screen?
    // Does the native SDK force the UIView to be within the screen for banners?
    // What happens for feeds in the native SDK? Does it not attach the banner until the UIView is
    // within the screen boundaries?
    return horizontalMargin;
}

- (int)calculateVerticalMargin:(VungleBannerPosition)adPosition withTopMargin:(int)topMargin withButtomMargin:(int)bottomMargin {
    // There is no such thing as "margins" for iOS native
    // So the implementation would be a vertical shift
    // Maybe only consider both if it's centered and top if it's top, etc
    int verticalMargin = topMargin - bottomMargin;

    // TODO: We should calculate margin based on "adPosition" not to display banner UIView outside of screen.
    // Should the plugin lock the UIView to within the screen?
    // Does the native SDK force the UIView to be within the screen for banners?
    // What happens for feeds in the native SDK? Does it not attach the banner until the UIView is
    // within the screen boundaries?
    return verticalMargin;
}

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
    int horizontalMargin = [self calculateHorizontalMargin:adPosition withLeftMargin:marginLeft withRightMargin:marginRight];
    int verticalMargin = [self calculateVerticalMargin:adPosition withTopMargin:marginTop withButtomMargin:marginBottom];

    // iOS 11 APIs
    if (@available(iOS 11.0, *)) {
        UIView* superview = adViewContainer.superview;
        adViewContainer.translatesAutoresizingMaskIntoConstraints = NO;
        NSMutableArray<NSLayoutConstraint*>* constraints = [NSMutableArray arrayWithArray:@[
            [adViewContainer.widthAnchor constraintEqualToConstant:CGRectGetWidth(adViewContainer.frame)],
            [adViewContainer.heightAnchor constraintEqualToConstant:CGRectGetHeight(adViewContainer.frame)],
        ]];

        switch (adPosition) {
            case TopLeft:
                [constraints addObjectsFromArray:@[[adViewContainer.topAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.topAnchor constant:verticalMargin],
                                                   [adViewContainer.leftAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.leftAnchor constant:horizontalMargin]]];
                break;
            case TopCenter:
                [constraints addObjectsFromArray:@[[adViewContainer.topAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.topAnchor constant:verticalMargin],
                                                   [adViewContainer.centerXAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerXAnchor constant:horizontalMargin]]];
                break;
            case TopRight:
                [constraints addObjectsFromArray:@[[adViewContainer.topAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.topAnchor constant:verticalMargin],
                                                   [adViewContainer.rightAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.rightAnchor constant:horizontalMargin]]];
                break;
            case Centered:
                [constraints addObjectsFromArray:@[[adViewContainer.centerXAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerXAnchor constant:horizontalMargin],
                                                   [adViewContainer.centerYAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerYAnchor constant:verticalMargin]]];
                break;
            case BottomLeft:
                [constraints addObjectsFromArray:@[[adViewContainer.bottomAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.bottomAnchor constant:verticalMargin],
                                                   [adViewContainer.leftAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.leftAnchor constant:horizontalMargin]]];
                break;
            case BottomCenter:
                [constraints addObjectsFromArray:@[[adViewContainer.bottomAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.bottomAnchor constant:verticalMargin],
                                                   [adViewContainer.centerXAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.centerXAnchor constant:horizontalMargin]]];
                break;
            case BottomRight:
                [constraints addObjectsFromArray:@[[adViewContainer.bottomAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.bottomAnchor constant:verticalMargin],
                                                   [adViewContainer.rightAnchor constraintEqualToAnchor:superview.safeAreaLayoutGuide.rightAnchor constant:horizontalMargin]]];
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
                viewContainerFrame.origin.x = 0 + horizontalMargin;
                viewContainerFrame.origin.y = 0 + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case TopCenter:
                viewContainerFrame.origin.x = (screenWidth / 2) - (viewContainerFrame.size.width / 2) + horizontalMargin;
                viewContainerFrame.origin.y = 0 + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case TopRight:
                viewContainerFrame.origin.x = screenWidth - viewContainerFrame.size.width + horizontalMargin;
                viewContainerFrame.origin.y = 0 + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case Centered:
                viewContainerFrame.origin.x = (screenWidth / 2) - (viewContainerFrame.size.width / 2) + horizontalMargin;
                viewContainerFrame.origin.y = (screenHeight / 2) - (viewContainerFrame.size.height / 2) + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleTopMargin | UIViewAutoresizingFlexibleBottomMargin);
                break;
            case BottomLeft:
                viewContainerFrame.origin.x = 0 + horizontalMargin;
                viewContainerFrame.origin.y = screenHeight - viewContainerFrame.size.height + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin);
                break;
            case BottomCenter:
                viewContainerFrame.origin.x = (screenWidth / 2) - (viewContainerFrame.size.width / 2) + horizontalMargin;
                viewContainerFrame.origin.y = screenHeight - viewContainerFrame.size.height + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin);
                break;
            case BottomRight:
                viewContainerFrame.origin.x = screenWidth - viewContainerFrame.size.width + horizontalMargin;
                viewContainerFrame.origin.y = screenHeight - viewContainerFrame.size.height + verticalMargin;
                adViewContainer.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleTopMargin);
                break;
            default:
                NSLog(@"An ad position is not specified.");
                break;
        }
        adViewContainer.frame = viewContainerFrame;
    }
}

@end
