//
//  MusicLibraryMediaPicker.h
//  UnityMPMusicPlayer
//

#import <AVFoundation/AVFoundation.h>
#import <MediaPlayer/MediaPlayer.h>

@interface MusicLibraryMediaPicker : NSObject {
    AVAudioPlayer *avPlayer;
    NSString *save_songTitle;
    NSString *save_artist;
    double all_duration;
}

@property (atomic, readonly) NSString* title;
@property (atomic, readonly) NSString* artist;
@property (atomic, readonly) double duration;
@property (atomic, readonly) double currentPlaybackTime;

+ (MusicLibraryMediaPicker*) shared;
- (void) presentMediaPicker:(UIViewController *)viewController;
- (void) selectedPicker;
- (double) getLevelWithChannel:(int) channel;
@end
