//
//  VungleBinding.m
//  Vungle Unity Plugin 6.8.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import "UnityInterface.h"
#import <VungleSDK/VungleSDK.h>
#import "VungleManager.h"
#import <AdSupport/ASIdentifierManager.h>

// Converts C style string to NSString
#define GetStringParam(_x_) (_x_ != NULL) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

// Converts C style string to NSString as long as it isnt empty
#define GetStringParamOrNil(_x_) (_x_ != NULL && strlen(_x_)) ? [NSString stringWithUTF8String:_x_] : nil

#define VUNGLE_API_KEY   @"vungle.api_endpoint"
#define VUNGLE_FILE_SYSTEM_SIZE_FOR_INIT_KEY  @"vungleMinimumFileSystemSizeForInit"
#define VUNGLE_MINIMUM_FILE_SYSTEM_SIZE_FOR_AD_REQUEST_KEY  @"vungleMinimumFileSystemSizeForAdRequest"
#define VUNGLE_MINIMUM_FILE_SYSTEM_SIZE_FOR_ASSET_DOWNLOAD_KEY  @"vungleMinimumFileSystemSizeForAssetDownload"

#pragma mark - SDKSetup

void _vungleStartWithAppId(const char * appId, const char * pluginVersion, BOOL initHeaderBiddingDelegate) {
    [[VungleManager sharedManager] initSDK:GetStringParam(appId) pluginName:GetStringParam(pluginVersion) headerBidding:initHeaderBiddingDelegate];
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

void _vungleEnableLogging(BOOL shouldEnable) {
    [[VungleSDK sharedSDK] setLoggingEnabled:shouldEnable];
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
    [VungleSDK sharedSDK].muted = !enabled;
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
    NSObject* obj = [VungleManager objectFromJson:GetStringParam(opt)];
    if([obj isKindOfClass:[NSDictionary class]]) {
        NSError * error;
        NSDictionary *from = obj;
        NSMutableDictionary *options = [NSMutableDictionary dictionary];
        options[VunglePlayAdOptionKeyOrientations] = @(makeOrientation(from[@"orientation"]));
        if (from[@"userTag"])
            options[VunglePlayAdOptionKeyUser]  = from[@"userTag"];
        if (from[@"alertTitle"])
            options[VunglePlayAdOptionKeyIncentivizedAlertTitleText] = from[@"alertTitle"];
        if (from[@"alertText"])
            options[VunglePlayAdOptionKeyIncentivizedAlertBodyText] = from[@"alertText"];
        if (from[@"closeText"])
            options[VunglePlayAdOptionKeyIncentivizedAlertCloseButtonText] = from[@"closeText"];
        if (from[@"continueText"])
            options[VunglePlayAdOptionKeyIncentivizedAlertContinueButtonText] = from[@"continueText"];
        if (from[@"flexCloseSec"])
            options[VunglePlayAdOptionKeyFlexViewAutoDismissSeconds] = from[@"flexCloseSec"];
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
    return [[VungleManager sharedManager] isBannerAvailable:GetStringParam(placementID) withSize:size];
}

void _vungleLoadBanner(const char* placementID, int bannerSize, int bannerPosition) {
    [[VungleManager sharedManager] requestBanner:GetStringParam(placementID) withSize:bannerSize atPosition:bannerPosition];
}

void _vungleSetMargins(const char* placementID, int marginLeft, int marginTop, int marginRight, int marginBottom) {
    [[VungleManager sharedManager] setMargins:GetStringParam(placementID) marginLeft:marginLeft marginTop:marginTop marginRight:marginRight marginBottom:marginBottom];
}

void _vungleShowBanner(const char* placementID) {
    [[VungleManager sharedManager] showBanner:GetStringParam(placementID)];
}

void _vungleCloseBanner(const char* placementID) {
    [[VungleManager sharedManager] closeBanner:GetStringParam(placementID)];
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

void _requestTrackingAuthorization() {
    [[VungleManager sharedManager] requestTrackingAuthorization];
}

#pragma mark - PrivateMethods

static char* MakeStringCopy (const char* string) {
    if (string == NULL) {
        return NULL;
    }

    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

#pragma mark - TestMethods

char * _vungleGetEndPoint() {
    NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
    return MakeStringCopy([[defaults objectForKey:VUNGLE_API_KEY] UTF8String]);
}

void _vungleSetEndPoint(const char * endPoint) {
    NSString *endPointString = GetStringParamOrNil(endPoint);
    if (endPointString) {
        NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
        [defaults setObject:endPointString forKey:VUNGLE_API_KEY];
    }
}

void _vungleClearSleep() {
    [[VungleSDK sharedSDK] clearSleep];
}

char * _vungleGetSdkVersion() {
    return MakeStringCopy([VungleSDKVersion UTF8String]);
}

char * _getIDFA() {
    NSString* identifier = [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
    return MakeStringCopy([identifier UTF8String]);
}
