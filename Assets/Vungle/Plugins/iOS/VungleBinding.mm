//
//  VungleBinding.mm
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import "VungleBanner.h"
#import "VungleBinding.h"
#import "VungleSDKDelegate.h"
#import "VungleUtility.h"
#import <VungleSDK/VungleSDK.h>
#import <AdSupport/ASIdentifierManager.h>

#if __has_include(<AppTrackingTransparency/ATTrackingManager.h>)
#import <AppTrackingTransparency/ATTrackingManager.h>
#endif

extern "C" void UnitySendMessage(const char *, const char *, const char *);
extern "C" UIViewController* UnityGetGLViewController();

// Converts C style string to NSString
#define GetStringParam(_x_) (_x_ != NULL) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

// Converts C style string to NSString as long as it isnt empty
#define GetStringParamOrNil(_x_) (_x_ != NULL && strlen(_x_)) ? [NSString stringWithUTF8String:_x_] : nil

#define VUNGLE_API_KEY   @"vungle.api_endpoint"
#define VUNGLE_FILE_SYSTEM_SIZE_FOR_INIT_KEY  @"vungleMinimumFileSystemSizeForInit"
#define VUNGLE_MINIMUM_FILE_SYSTEM_SIZE_FOR_AD_REQUEST_KEY  @"vungleMinimumFileSystemSizeForAdRequest"
#define VUNGLE_MINIMUM_FILE_SYSTEM_SIZE_FOR_ASSET_DOWNLOAD_KEY  @"vungleMinimumFileSystemSizeForAssetDownload"

NSMutableDictionary *vungleAds = [[NSMutableDictionary alloc] init];
BOOL soundEnabled = YES;

static char* MakeStringCopy (const char* string) {
    if (string == NULL) {
        return NULL;
    }

    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

char * _vungleGetSdkVersion() {
    return MakeStringCopy([VungleSDKVersion UTF8String]);
}

void _vungleEnableLogging(BOOL shouldEnable) {
    [VungleUtility setLoggingEnabled:shouldEnable];
    [[VungleSDK sharedSDK] setLoggingEnabled:shouldEnable];
}

#pragma mark - Initialization

void _vungleStartWithAppId(const char * appId, const char * pluginVersion, BOOL initHeaderBiddingDelegate) {
    if (![VungleSDK sharedSDK].initialized) {
        if ([[VungleSDK sharedSDK] respondsToSelector:@selector(setPluginName:version:)]) {
            [[VungleSDK sharedSDK] performSelector:@selector(setPluginName:version:) withObject:@"unity" withObject:GetStringParam(pluginVersion)];
        }

        [VungleSDK sharedSDK].delegate = [VungleSDKDelegate instance];
        [[VungleSDK sharedSDK] setLoggingEnabled:true];
        [[VungleSDK sharedSDK] attachLogger:[VungleSDKDelegate instance]];
        [VungleSDK sharedSDK].creativeTrackingDelegate = [VungleSDKDelegate instance];
        if (initHeaderBiddingDelegate) {
            [VungleSDK sharedSDK].headerBiddingDelegate = [VungleSDKDelegate instance];
        }

        NSError * error;
        if (![[VungleSDK sharedSDK] startWithAppId:GetStringParam(appId) error:&error]) {
            NSString *message = [NSString stringWithFormat:@"Failed to initialize SDK - %@",
                                 [error localizedDescription]];
            [VungleUtility sendErrorMessage:message];
        }
    }
}

BOOL _vungleIsInitialized() {
    return [[VungleSDK sharedSDK] isInitialized];
}

void _vungleSetPublishIDFV(BOOL shouldEnable) {
    [VungleSDK setPublishIDFV:shouldEnable];
}

void _vungleSetMinimumSpaceForInit(int minimumSize) {
    NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
    [defaults setInteger:minimumSize forKey:VUNGLE_FILE_SYSTEM_SIZE_FOR_INIT_KEY];
}

void _vungleSetMinimumSpaceForAd(int minimumSize) {
    NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];

    [defaults setInteger:minimumSize forKey:VUNGLE_MINIMUM_FILE_SYSTEM_SIZE_FOR_AD_REQUEST_KEY];
    [defaults setInteger:minimumSize forKey:VUNGLE_MINIMUM_FILE_SYSTEM_SIZE_FOR_ASSET_DOWNLOAD_KEY];
}

#pragma mark - ConsentStatus

void _updateConsentStatus(int status, const char * version) {
    if (status == 1) {
        [[VungleSDK sharedSDK] updateConsentStatus:VungleConsentAccepted consentMessageVersion:GetStringParam(version)];
    } else if (status == 2) {
        [[VungleSDK sharedSDK] updateConsentStatus:VungleConsentDenied consentMessageVersion:GetStringParam(version)];
    }
}

int _getConsentStatus() {
    VungleConsentStatus consent = [[VungleSDK sharedSDK] getCurrentConsentStatus];
    if (consent == NULL) return 0;
    return (consent == VungleConsentAccepted) ? 1 : 2;
}

#pragma mark - PlaybackOptions

UIInterfaceOrientationMask makeOrientation(NSNumber* code) {
    UIInterfaceOrientationMask orientationMask;
    int i = [code intValue];
    switch(i) {
        case 1:
            orientationMask = UIInterfaceOrientationMaskPortrait;
            break;
        case 2:
            orientationMask = UIInterfaceOrientationMaskLandscapeLeft;
            break;
        case 3:
            orientationMask = UIInterfaceOrientationMaskLandscapeRight;
            break;
        case 4:
            orientationMask = UIInterfaceOrientationMaskPortraitUpsideDown;
            break;
        case 5:
            orientationMask = UIInterfaceOrientationMaskLandscape;
            break;
        case 6:
            orientationMask = UIInterfaceOrientationMaskAll;
            break;
        case 7:
            orientationMask = UIInterfaceOrientationMaskAllButUpsideDown;
            break;
        default:
            orientationMask = UIInterfaceOrientationMaskAllButUpsideDown;
    }
    return orientationMask;
}

void _vungleSetSoundEnabled(BOOL enabled) {
    soundEnabled = enabled;
    [VungleSDK sharedSDK].muted = !enabled;
}

BOOL _vungleIsSoundEnabled() {
    return soundEnabled;
}

#pragma mark - AdLifecycle-Fullscreen // aki:todo - move to Manager class

BOOL _vungleIsAdAvailable(const char* placementID) {
    return [[VungleSDK sharedSDK] isAdCachedForPlacementID:GetStringParam(placementID)];
}

BOOL _vungleLoadAd(const char* placementID) {
    NSError * error;
    return [[VungleSDK sharedSDK] loadPlacementWithID:GetStringParam(placementID) error:&error];
}

void _vunglePlayAd(char* opt, const char* placementID) {
    NSObject* obj = [VungleUtility objectFromJson:GetStringParam(opt)];
    if([obj isKindOfClass:[NSDictionary class]]) {
        NSError * error;
        NSDictionary *from = obj;
        NSMutableDictionary *options = [NSMutableDictionary dictionary];
        options[VunglePlayAdOptionKeyOrientations] = @(makeOrientation(from[@"orientation"]));
        if ([from objectForKey:@"muted"])
            options[VunglePlayAdOptionKeyStartMuted] = from[@"muted"];
        if (from[@"userTag"])
            options[VunglePlayAdOptionKeyUser] = from[@"userTag"];
        if (from[@"alertTitle"])
            options[VunglePlayAdOptionKeyIncentivizedAlertTitleText] = from[@"alertTitle"];
        if (from[@"alertText"])
            options[VunglePlayAdOptionKeyIncentivizedAlertBodyText] = from[@"alertText"];
        if (from[@"closeText"])
            options[VunglePlayAdOptionKeyIncentivizedAlertCloseButtonText] = from[@"closeText"];
        if (from[@"continueText"])
            options[VunglePlayAdOptionKeyIncentivizedAlertContinueButtonText] = from[@"continueText"];
        if (from[@"ordinal"])
            options[VunglePlayAdOptionKeyOrdinal] = [NSNumber numberWithUnsignedInteger:[from[@"ordinal"] integerValue]];
        [[VungleSDK sharedSDK] playAd:UnityGetGLViewController() options:options placementID:GetStringParam(placementID) error:&error];
    }
}

BOOL _vungleCloseAd(const char* placementID) {
    [[VungleSDK sharedSDK] finishDisplayingAd:GetStringParam(placementID)];
    return TRUE;
}

#pragma mark - AdLifecycle-Banner

BOOL _vungleIsBannerAvailable(const char* placementID, int size) {
    NSString *placement = GetStringParam(placementID);
    VungleBanner *banner = [vungleAds objectForKey:placement];
    // Unity plugin doesn't know the autocached values and wanted placement
    // so if it isn't loaded beforehand, return false
    if (!banner) {
        return false;
    }
    if (size == VunglePluginAdSizeBannerMrec) {
        return [[VungleSDK sharedSDK] isAdCachedForPlacementID:placement];
    }
    return [[VungleSDK sharedSDK] isAdCachedForPlacementID:placement withSize:[VungleBanner getVungleBannerSize:size]];
}

void _vungleLoadBanner(const char* placementID, int bannerSize, int bannerPosition) {
    NSString *placement = GetStringParam(placementID);
    VungleBanner *banner = [vungleAds objectForKey:placement];
    if (!banner) {
        banner = [[VungleBanner alloc] initWithPlacement:placement size:bannerSize position:bannerPosition viewController:UnityGetGLViewController()];
        [vungleAds setObject:banner forKey:placement];
    } else {
        banner->bannerSize = bannerSize;
        banner->bannerPosition = bannerPosition;
    }
    [banner loadBanner];
}

void _vungleSetOffset(const char* placementID, int x, int y) {
    NSString *placement = GetStringParam(placementID);
    VungleBanner *banner = [vungleAds objectForKey:placement];
    if (!banner) {
        NSString *message = [NSString stringWithFormat:@"Vungle: Failed to set offset; banner %@ not loaded", placement];
        [VungleUtility sendErrorMessage:message];
        return;
    }
    [banner setOffset:x y:y];
}

void _vungleShowBanner(const char* placementID, const char* options) {
    NSString *placement = GetStringParam(placementID);
    VungleBanner *banner = [vungleAds objectForKey:placement];
    if (!banner) {
        NSString *message = [NSString stringWithFormat:@"Vungle: Failed to show banner; banner %@ not loaded", placement];
        [VungleUtility sendErrorMessage:message];
        return;
    }
    NSString *opt = GetStringParam(options);
    [banner showBanner:opt];
}

void _vungleCloseBanner(const char* placementID) {
    NSString *placement = GetStringParam(placementID);
    VungleBanner *banner = [vungleAds objectForKey:placement];
    if (!banner) {
        NSString *message = [NSString stringWithFormat:@"Vungle: Failed to close banner; banner %@ not found", placement];
        [VungleUtility sendErrorMessage:message];
        return;
    }
    [banner closeBanner];
    [vungleAds removeObjectForKey:placement];
}

#pragma mark - CCPA

void _updateCCPAStatus(int status) {
    if (status == 1) {
        [[VungleSDK sharedSDK] updateCCPAStatus:VungleCCPAAccepted];
    } else if (status == 2) {
        [[VungleSDK sharedSDK] updateCCPAStatus:VungleCCPADenied];
    }
}

int _getCCPAStatus() {
    VungleCCPAStatus status = [[VungleSDK sharedSDK] getCurrentCCPAStatus];
    if (status == NULL) return 0;
    return (status == VungleCCPAAccepted) ? 1 : 2;
}

#pragma mark - AppTrackingTransparency iOS14

void _requestTrackingAuthorization() {
    if (@available(iOS 14, *)) {
#if __has_include(<AppTrackingTransparency/ATTrackingManager.h>)
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            UnitySendMessage("VungleManager", "TrackingCallback", [[@(status) stringValue] UTF8String]);
        }];
#else
        // Not Determined since the framework is not part of the xcode app (xcode 11 and below)
        UnitySendMessage("VungleManager", "TrackingCallback", "0");
#endif
    } else {
        // Assume good to go since not iOS 14?
        UnitySendMessage("VungleManager", "TrackingCallback", "3");
    };
}
