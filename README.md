# Marathon Environments

A set of high-dimensional continuous control environments for use with Unity ML-Agents Toolkit.

![MarathonEnvs](images/MarathonEnvsBanner.gif)

## Preview MarathonEnvs using the **[Web Demo](http://marathonenvs.joebooth.com)**

**MarathonEnvs** is a set of high-dimensional continuous control benchmarks using Unity’s native physics simulator, PhysX. MarathonEnvs can be trained using Unity ML-Agents or any OpenAI Gym compatable algorthem. MarathonEnvs maybe useful for:

* Video Game researchers interested in apply bleeding edge robotics research into the domain of locomotion and AI for video games.
* Academic researchers looking to leverage the strengths of Unity and ML-Agents along with the body of existing research and benchmarks provided by projects such as the [DeepMind Control Suite](https://github.com/deepmind/dm_control), or [OpenAI Mujoco](http://gym.openai.com/envs/#mujoco) environments.

*Note: This project is the result of a contribution from [Joe Booth (@Sohojo)](https://github.com/Sohojoe), a member of the Unity community who currently maintains the repository. As such, the contents of this repository are not officially supported by Unity Technologies.*

---

## What's new in MarathonEnvs-v2.0.0-alpha.2

### WebGL Demo / Support for in browser

* See [Web Demo](http://marathonenvs.joebooth.com)

### marathon-envs Gym wrapper (Preview)

* Use marathon-envs as a OpenAI Gym environment - see [documentation](marathon-envs/README.md)

### ml-agents 0.14.1 support

* Updated to work with ml-agents 0.14.1 / new inference engine

### Unity 2018.4 LTS

* Updated to use Unity 2018.4 LTS. Should work with later versions. However, sometimes Unity makes breaking physics changes.

### MarathonManBackflip-v0

* Train the agent to complete a backflip based on motion capture data
* Merged from StyleTransfer experimental repro


### MarathonMan-v0

* Optimized for Unity3d + fixes some bugs with the DeepMind.xml version
* Merged from StyleTransfer experimental repro
* Replaces DeepMindHumanoid

### ManathonManSparse-v0

* Sparse version of MarathonMan.
* Single reward is given at end of episode.

### TerrainHopperEnv-v0, TerrainWalker2dEnv-v0, TerrainAntEnv-v0, TerrainMarathonManEnv-v0

* Random Terrain envionments
* Merged from AssaultCourse experimental repro

### SpawnableEnvs (Preview)

* Set the number of instances of an envrionmwnt you want for training and inference
* Envrionments are spawned from prefabs, so no need to manually duplicate
* Supports ability to select from multiple agents in one build
* Unique Physics Scene per Environment (makes it easier to port envionments however runs slower)
* SelectEnvToSpawn.cs - Optional menu to enable user to select from all agents in build

### Scorer.cs

* Score agent against 'goal' (for example, max distance) to distinguish rewards from goals
* Gives mean and std-div over 100 agents

### Normalized Observations (-1 to 1) and reward (0 to 1)

* No need to use normalize flag in training. Helps with OpenAI.Baselines training

### Merge CameraHelper.cs from StyleTransfer. Controls are

* 1, 2, 3 - Slow-mo modes
* arrow keys or w-a-s-d rotate around agent
* q-e zoom in / out

### Default hyperparams are now closer to OpenAI.Baselines

* (1m steps for hopper, walker, ant, 10m for humanoid)

### Training speed improvements - All feet detect distance from floor

## Getting Started

### Requirements

* Unity 2018.4 (Download [here](https://unity3d.com/get-unity/download)).
* Cloan / Download this repro
* Install ml-agents version 0.14.1 - install via:

``` sh
pip3 install mlagents==0.14.1
```

* Build or install the correct runtime for your version into the `envs\` folder

### Training

* See [Training.md](Training.md) for training us ML-Agents

---

## Publications & Usage

### Publications

* AAAI 2019 Workshop on Games and Simulations for Artificial Intelligence: [Marathon Environments: Multi-Agent Continuous Control Benchmarks in a Modern Video Game Engine](https://arxiv.org/abs/1902.09097)
* An early version of this work was presented March 19th, 2018 at the AI Summit - [Game Developer Conference 2018](http://schedule.gdconf.com/session/beyond-bots-making-machine-learning-accessible-and-useful/856147)

### Research using ML-Agents + MarathonEnvs

* [ActiveRagdollAssaultCourse](https://github.com/Sohojoe/ActiveRagdollAssaultCourse) - Mastering Dynamic Environments
* [ActiveRagdollControllers](https://github.com/Sohojoe/ActiveRagdollControllers) - Implementing a Player Controller
* [ActiveRagdollStyleTransfer](https://github.com/Sohojoe/ActiveRagdollStyleTransfer) - Learning From Motioncapture Data
* [MarathonEnvsBaselines](https://github.com/Sohojoe/MarathonEnvsBaselines) - Experimental implementation with OpenAI.Baselines and Stable.Baselines

---

## References

* [OpenAI.Gym Mujoco](https://github.com/openai/gym/tree/master/gym/envs/mujoco) implementation. Good reference for enviroment setup, reward functions and termination functions.
* [OpenAI.Roboschool](https://github.com/openai/roboschool) - Alternative OpenAI implementation based on [Bullet Physics](http://pybullet.org) with more advanced enviroments. Alternative reference for reward functions and termination functions.
* [DeepMind Control Suite](https://github.com/deepmind/dm_control) - Set of continuous control tasks.
* DeepMind paper [Emergence of Locomotion Behaviours in Rich Environments](https://arxiv.org/pdf/1707.02286) and [video](https://youtu.be/hx_bgoTF7bs)- see page 13 b.2 for detail of reward functions
* [MuJoCo](http://www.mujoco.org) homepage.
* A good primer on the differences between physics engines is ['Physics simulation engines have traditional made tradeoffs between performance’](https://homes.cs.washington.edu/~todorov/papers/ErezICRA15.pdf) and it’s accompanying [video](https://homes.cs.washington.edu/~todorov/media/ErezICRA15.mp4).
* [MuJoCo Unity Plugin](http://www.mujoco.org/book/unity.html) MuJoCo's Unity plugin which uses socket to comunicate between MuJoCo (for running the physics simulation and control) and Unity (for rendering).

### Citing MarathonEnvs

If you use MarathonEnvs in your research, we ask that you please cite our [paper](https://arxiv.org/abs/1902.09097).
