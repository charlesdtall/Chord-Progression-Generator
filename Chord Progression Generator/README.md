This app will eventually generate chord progressions that are statistically likely to sound pleasing. To do this, it will randomly select sequences of chords which each choice being weighted based on how common it is, which can be narrowed down using filters by genre, period, progression length, etc.

Notes are represented by numbers 0 - 11. Chords rooted on 0 default to I so that progressions can be key-agnostic, and C because some progressions in the database use chord symbols transposed to C and some us RN Analysis, idk, we'll see.

Eventually I want to also have the app recognize relative motion as being equivalent. For example, the Axis Progression (I-V-vi-IV) is commonly started on I and vi. Starting on vi, it's a minor progression that could also be analyzed as i-bVI-bIII-bVII. Not so sure how to get this code to recognize that I-V-vi-IV = i-bVI-bIII-bVII, but recognizing relative relationships like this is important to Chord Loop Theory (see [Patricia Taxxon's video](https://www.youtube.com/watch?v=K-XSTSnqXxo) on the topic for more info).

### Goals

- Create a UI, obviously
- Have it produce a modulation between two key centers based how abrupt you want it to be (maybe Schoenberg's "region distances" from *Structural Functions of Harmony* will be useful here), which might include chaining modulations together (Circle of 5ths, sequences, etc.). Also, pivot chords (have to check common tones of keys and translate to chord possibilities? Have database of chords in each key?)
- Use Markov model for progression generator: "Based on this chord, how what chord is likely to come next". Still need to decide if I want this to be specific to key center (V7 is most likely going to I) or flexible (a Major chord is most likely to go to X chord). This might depend on "type" of progression (functional progressions use the former, loops use the latter)