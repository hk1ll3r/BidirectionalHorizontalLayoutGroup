# BidirectionalHorizontalLayoutGroup
A Unity3D horizontal layout group that supports both left-to-right and right-to-left. Useful if you are building a UI for an RTL language such as Arabic, Persian, Hebrew, etc. or if you support multiple languages in your app.

The component works just like the default HorizontalLayoutGroup. The only difference is the addition of the "reverse" field. When "reverse" is checked, the layout is done from right to left.

![BidirectionalHorizontalLayoutGroup in action](https://raw.githubusercontent.com/hk1ll3r/ShowcaseOfBidirectionalHorizontalLayoutGroup/master/Resources/vid1.gif)

The implementation is based on Unity's HorizontalLayoutGroup located below.
https://bitbucket.org/Unity-Technologies/ui/src/2019.1/UnityEngine.UI/UI/Core/Layout/LayoutGroup.cs


## Notes
The Left and Right padding fields always refer to Left and Right respectively regardless of whether "reverse" is checked. However "children alignment" field gets reversed along with the children's order. Take the following examples.

### LTR mode
![BidirectionalHorizontalLayoutGroup in normal LTR mode](https://raw.githubusercontent.com/hk1ll3r/ShowcaseOfBidirectionalHorizontalLayoutGroup/master/Resources/vid2.gif)

### RTL mode
![BidirectionalHorizontalLayoutGroup in reverse mode](https://raw.githubusercontent.com/hk1ll3r/ShowcaseOfBidirectionalHorizontalLayoutGroup/master/Resources/vid3.gif)
