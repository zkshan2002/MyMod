
### The Intelligence
Machine version of Brain of Cuthulhu. It has 7 guards that can either act independently, or be commanded to do joint action.

#### Todo
##### Visual
1. find a better sprite for both brain and guard.
2. rethink all alerts. Alert for invade/fast invade/fast laser/deathray zap/deathray sweep should all be different. 
3. lasers and deathrays can look better. Lasers should be at least something similar to martian saucer's laser attack. Deathrays should be glowy and a bit more blurry.
4. mirror images should look better, and also should not be always 4 mirroring images.
5. enter animation and death animation.
##### AI
1. teleportAndShoot1-3/2-3 too hard and too complex. Especially 2-3 where rotating deathrays are not Terraria.
2. stage 3 currently very disappointing. It should be at least not so vanilla, has at least 2 attack modes, and be possible to dodge.
3. more attack of brain is expected. For instance, teleport to above player, sucks player towards it, and launch large energy balls. Or, blast off player with shock waves, then immediately teleport to near player for following attacks.
4. fast invade/retreat needs better considering.
5. before and after charge is another thing to be reconsidered. In vanilla AI, consecutive charge contains charge and recover, but before first charge there's a fly-to-target part. Skipping this part will cause some charge to happen at somewhere too far to pose actual threat.
6. timing. Currently all attacks go fast and thus player is too passive to fight back. Perhaps adding some leisure time between locked time table could help? Also, waiting invading guards to return is dumb. Find a smarter way to solve this.

### Brainstorm

#### Machine King Slime

features of slime:
- periodically jumps. Helpless in midair.
- able to fly(Queen Slime, etc.).
- reform?(King Slime summons small slimes, Slime God in Calamity splits at fewer health)

references:
- King Slime, Queen Slime, Slime God, Astral Slime
- Biome mimics, Golem

questions:
- how does a slime land in midair?
- how does a slime align to same height with player before takes a jump?

#### Machine Queen Bee

features of bee:
- agily flies.
- shoots stings.