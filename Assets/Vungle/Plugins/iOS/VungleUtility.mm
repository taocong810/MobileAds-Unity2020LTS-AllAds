//
//  VungleUtility.mm
//  Vungle Unity Plugin 6.9.0
//
//  Copyright (c) 2013-Present Vungle Inc. All rights reserved.
//

#import <AVFoundation/AVAudioSession.h>
#import <Foundation/Foundation.h>

#import "VungleUtility.h"

#if __has_feature(objc_arc)
#define SAFE_ARC_AUTORELEASE(x) (x)
#else
#define SAFE_ARC_AUTORELEASE(x) ([(x) autorelease])
#endif

extern "C" void UnitySendMessage(const char *, const char *, const char *);
extern "C" void UnitySetAudioSessionActive(int active);

@implementation VungleUtility

AVAudioSessionCategory audioCategory;
AVAudioSessionCategoryOptions audioCategoryOptions;
BOOL categoryIsSet = NO;
BOOL isLoggingEnabled = NO;

+ (NSString*)jsonFromObject:(id)object {
    NSError *error = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:object options:0 error:&error];

    if (jsonData && !error) {
        return SAFE_ARC_AUTORELEASE([[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding]);
    }

    if (error) {
        NSString *message = [NSString stringWithFormat:@"Failed to create JSON - %@",
                             [error localizedDescription]];
        [VungleUtility sendErrorMessage:message];
    }

    return @"{}";
}

+ (id)objectFromJson:(NSString*)json {
    NSError *error = nil;
    NSData *data = [NSData dataWithBytes:json.UTF8String length:json.length];
    NSObject *object = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingAllowFragments error:&error];
    if (error) {
        NSString *message = [NSString stringWithFormat:@"Failed to create object from JSON - %@",
                             [error localizedDescription]];
        [VungleUtility sendErrorMessage:message];
    }

    return object;
}

+ (void)pauseAudioSession {
    UnitySetAudioSessionActive(FALSE);
    categoryIsSet = YES;
    audioCategory = [[AVAudioSession sharedInstance] category];
    audioCategoryOptions = [[AVAudioSession sharedInstance] categoryOptions];
    [[AVAudioSession sharedInstance] setCategory:AVAudioSessionCategorySoloAmbient error:NULL];
    [[AVAudioSession sharedInstance] setActive:YES withOptions:AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers error:NULL];
}

+ (void)revertAudioSession {
    if (categoryIsSet) {
        categoryIsSet = NO;
        [[AVAudioSession sharedInstance] setActive:NO withOptions:AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers error:NULL];
        [[AVAudioSession sharedInstance] setCategory:audioCategory withOptions:audioCategoryOptions error:NULL];
        UnitySetAudioSessionActive(TRUE);
    }
}

+ (void)setLoggingEnabled:(BOOL)isEnabled {
    isLoggingEnabled = isEnabled;
}

+ (void)sendLog:(NSString*)message {
    if (isLoggingEnabled) {
        UnitySendMessage("VungleManager", "OnSDKLog", [message UTF8String]);
    }
}

+ (void)sendWarningMessage:(NSString*)message {
    UnitySendMessage("VungleManager", "OnWarning", [message UTF8String]);
}

+ (void)sendErrorMessage:(NSString*)message {
    UnitySendMessage("VungleManager", "OnError", [message UTF8String]);
}

@end
